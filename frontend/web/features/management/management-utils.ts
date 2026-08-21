export function initials(name = "") {
  return name.split(" ").filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase();
}

export async function cropPhoto4x6(file: File): Promise<string> {
  const source = await new Promise<string>((resolve, reject) => { const reader = new FileReader(); reader.onload = () => resolve(String(reader.result)); reader.onerror = () => reject(reader.error); reader.readAsDataURL(file); });
  const image = await new Promise<HTMLImageElement>((resolve, reject) => { const value = document.createElement("img"); value.onload = () => resolve(value); value.onerror = reject; value.src = source; });
  const canvas = document.createElement("canvas"); canvas.width = 400; canvas.height = 600;
  const context = canvas.getContext("2d"); if (!context) return source;
  const targetRatio = 2 / 3; const sourceRatio = image.width / image.height;
  let sx = 0, sy = 0, sw = image.width, sh = image.height;
  if (sourceRatio > targetRatio) { sw = image.height * targetRatio; sx = (image.width - sw) / 2; } else { sh = image.width / targetRatio; sy = (image.height - sh) / 2; }
  context.drawImage(image, sx, sy, sw, sh, 0, 0, 400, 600);
  return canvas.toDataURL("image/jpeg", .84);
}
