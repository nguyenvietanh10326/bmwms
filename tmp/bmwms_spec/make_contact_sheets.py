from pathlib import Path
import sys

from PIL import Image, ImageDraw


root = Path(__file__).resolve().parent
render_name = sys.argv[1] if len(sys.argv) > 1 else "render_pdf"
contact_name = sys.argv[2] if len(sys.argv) > 2 else "contact_sheets"
pages = sorted((root / render_name).glob("page-*.png"))
out = root / contact_name
out.mkdir(parents=True, exist_ok=True)

for group_index in range(0, len(pages), 4):
    group = pages[group_index : group_index + 4]
    opened = [Image.open(path).convert("RGB") for path in group]
    width = max(image.width for image in opened)
    height = max(image.height for image in opened)
    sheet = Image.new("RGB", (width * 2 + 60, height * 2 + 100), "#DDE3EA")
    draw = ImageDraw.Draw(sheet)
    for index, (path, image) in enumerate(zip(group, opened)):
        x = 20 + (index % 2) * (width + 20)
        y = 40 + (index // 2) * (height + 40)
        sheet.paste(image, (x, y))
        draw.text((x, y - 25), path.stem, fill="#17365D")
    start = group_index + 1
    end = group_index + len(group)
    sheet.save(out / f"pages-{start:02d}-{end:02d}.png")
