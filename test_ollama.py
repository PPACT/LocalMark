"""测试 OllamaAgent 标注效果 —— 调 Ollama API + 画框 + 自动打开图片"""
import sys, os, json, time, re, base64, requests
from PIL import Image, ImageDraw, ImageFont
from io import BytesIO

MODEL = "qwen2.5vl:7b"
OLLAMA = "http://localhost:11434"
IMAGE_PATH = sys.argv[1] if len(sys.argv) > 1 else r"D:\Code\Document\测试用例\交叉路口.jpg"
LABELS = ["行人", "机动车", "非机动车"]
NUM_PREDICT = int(sys.argv[2]) if len(sys.argv) > 2 else 2048

# 不同标签用不同颜色画框
COLORS = {
    "行人": "#00FF00",   # 绿
    "机动车": "#FF6600", # 橙
    "非机动车": "#0099FF", # 蓝
}
FALLBACK_COLOR = "#FF0000"  # 红（未知标签）

def resize_image(path, max_dim=1024):
    img = Image.open(path)
    w, h = img.size
    m = max(w, h)
    if m <= max_dim:
        return img, 1.0
    scale = max_dim / m
    img = img.resize((int(w*scale), int(h*scale)), Image.LANCZOS)
    return img, scale

def parse_response(text, valid_labels):
    """模拟 C# TryParseJson + FallbackParse"""
    results = []

    # 格式1: 标准数组 [{...}]
    try:
        data = json.loads(text)
        if isinstance(data, list):
            for item in data:
                results.append(to_obj(item, valid_labels))
            return ("标准数组", results)
    except: pass

    # 格式2: V3 字典 {"标签": [{...}]}
    try:
        data = json.loads(text)
        if isinstance(data, dict):
            for label, boxes in data.items():
                if isinstance(boxes, list):
                    for box in boxes:
                        results.append(to_obj({"label": label, "bbox_2d": box.get("bbox_2d", box.get("bbox", []))}, valid_labels))
            return ("V3字典", results)
    except: pass

    # 格式3: fallback 正则
    matches = re.findall(r'\{[^{}]*?"label"[^{}]*?"bbox_2d"[^{}]*?\[[^\]]*\][^{}]*?\}', text)
    for m in matches:
        try:
            item = json.loads(m)
            results.append(to_obj(item, valid_labels))
        except: pass
    return ("fallback正则", results)

def to_obj(raw, valid_labels):
    bbox = raw.get("bbox_2d", [0,0,0,0])
    if len(bbox) < 4:
        return None
    x1, y1, x2, y2 = bbox[0], bbox[1], bbox[2], bbox[3]
    if x1 > x2: x1, x2 = x2, x1
    if y1 > y2: y1, y2 = y2, y1
    label = raw.get("label", "?")
    match = next((l for l in valid_labels if l == label), label)
    return {"label": match, "x": x1, "y": y1, "w": x2-x1, "h": y2-y1}

def dedup(results):
    seen = set()
    unique = []
    for r in results:
        if r is None:
            continue
        key = (r["label"], round(r["x"]), round(r["y"]), round(r["w"]), round(r["h"]))
        if key not in seen:
            seen.add(key)
            unique.append(r)
    return unique

def draw_boxes(image, detections):
    """在图片上画框 + 标签文字"""
    draw = ImageDraw.Draw(image)
    # 尝试加载中文字体
    font = None
    for fp in ["C:/Windows/Fonts/msyh.ttc", "C:/Windows/Fonts/simhei.ttf", "C:/Windows/Fonts/simsun.ttc"]:
        try:
            font = ImageFont.truetype(fp, 16)
            break
        except:
            pass

    for d in detections:
        color = COLORS.get(d["label"], FALLBACK_COLOR)
        x1, y1 = d["x"], d["y"]
        x2, y2 = d["x"] + d["w"], d["y"] + d["h"]
        # 画矩形框（加粗：画3层）
        for offset in range(3):
            draw.rectangle([x1-offset, y1-offset, x2+offset, y2+offset], outline=color)
        # 画标签背景+文字
        label_text = f"{d['label']}"
        if font:
            bbox = draw.textbbox((x1, y1-20), label_text, font=font)
            draw.rectangle(bbox, fill=color)
            draw.text((x1, y1-20), label_text, fill="white", font=font)
        else:
            draw.rectangle([x1, y1-18, x1+60, y1], fill=color)
            draw.text((x1+2, y1-17), label_text, fill="white")

    return image

def main():
    print(f"图片: {os.path.basename(IMAGE_PATH)}")
    # 发原图，不缩放——模型返回的坐标直接就是原图像素坐标
    full_img = Image.open(IMAGE_PATH)
    fw, fh = full_img.size
    print(f"尺寸: {fw}x{fh}")

    buf = BytesIO()
    full_img.save(buf, format="JPEG", quality=85)
    img_b64 = base64.b64encode(buf.getvalue()).decode()

    label_list = "、".join(LABELS)
    prompt = (
        "逐一检查这张图片中的所有目标，每个目标独立标注一个框。"
        f"使用以下{len(LABELS)}个标签：{label_list}。"
        "标注规则：每个目标单独画框（不可合并），可见部分超过50%才标注。"
        "输出JSON数组，格式："
        '[{"label": "标签名", "bbox_2d": [x1,y1,x2,y2]}]。'
        "确保 x1<x2 且 y1<y2。"
    )

    print("调用 Ollama 中...")
    t0 = time.time()
    resp = requests.post(f"{OLLAMA}/api/generate", json={
        "model": MODEL,
        "prompt": prompt,
        "images": [img_b64],
        "format": "json",
        "stream": False,
        "options": {"num_predict": NUM_PREDICT}
    }, timeout=300)
    t1 = time.time()

    data = resp.json()
    raw = data.get("response", "")
    done = data.get("done", False)
    eval_count = data.get("eval_count", 0)
    eval_dur = data.get("eval_duration", 0) / 1e9

    # 解析 + 去重
    method, results = parse_response(raw, LABELS)
    unique = dedup(results)

    # 发的是原图，坐标已经是原图像素值，只需裁剪到边界内
    for r in unique:
        r["x"] = max(0, min(r["x"], fw - 1))
        r["y"] = max(0, min(r["y"], fh - 1))
        r["w"] = max(1, min(r["w"], fw - r["x"]))
        r["h"] = max(1, min(r["h"], fh - r["y"]))

    # 统计
    by_label = {}
    for r in unique:
        by_label[r["label"]] = by_label.get(r["label"], 0) + 1

    print(f"耗时: {t1-t0:.1f}s | tokens: {eval_count} | done: {done}")
    print(f"解析方式: {method} | 原始片段: {len(results)} | 去重后: {len(unique)}")
    print(f"标签分布: {by_label}")
    for i, r in enumerate(unique):
        print(f"  [{i}] {r['label']} @ ({r['x']:.0f},{r['y']:.0f}) {r['w']:.0f}x{r['h']:.0f}")

    # 画框并保存
    annotated = draw_boxes(full_img.copy(), unique)
    out_path = os.path.splitext(IMAGE_PATH)[0] + "_标注结果.png"
    annotated.save(out_path)
    print(f"\n标注结果已保存: {out_path}")
    os.startfile(out_path)

if __name__ == "__main__":
    main()
