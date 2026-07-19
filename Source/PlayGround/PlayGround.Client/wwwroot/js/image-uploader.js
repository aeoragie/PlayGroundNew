// 이미지 업로더 공용 — Design.ImageUploader
// 업로드 전에 브라우저에서 리사이즈(장변 2000px) + EXIF 회전 보정을 끝낸다.
// 용량 초과 대부분을 사전 방지하고, 세로로 찍은 사진이 눕는 문제도 여기서 정리한다.

const MAX_EDGE = 2000;
const JPEG_QUALITY = 0.88;
const ALLOWED = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_BYTES = 10 * 1024 * 1024;

// createImageBitmap의 imageOrientation:'from-image'가 EXIF를 알아서 적용해 준다.
// 미지원 브라우저는 EXIF를 직접 읽어 캔버스 변환으로 보정한다.
async function loadOrientedBitmap(file) {
  if ('createImageBitmap' in window) {
    try {
      return { bitmap: await createImageBitmap(file, { imageOrientation: 'from-image' }), orientation: 1 };
    } catch {
      // 아래 폴백으로
    }
  }

  const orientation = await readExifOrientation(file);
  const bitmap = await loadImageElement(file);
  return { bitmap, orientation };
}

function loadImageElement(file) {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const image = new Image();
    image.onload = () => { URL.revokeObjectURL(url); resolve(image); };
    image.onerror = () => { URL.revokeObjectURL(url); reject(new Error('image decode failed')); };
    image.src = url;
  });
}

// EXIF Orientation 태그(0x0112)만 읽는다 — 전체 파서를 들일 이유가 없다.
async function readExifOrientation(file) {
  const head = await file.slice(0, 128 * 1024).arrayBuffer();
  const view = new DataView(head);
  if (view.byteLength < 4 || view.getUint16(0) !== 0xffd8) {
    return 1; // JPEG가 아니면 EXIF도 없다
  }

  let offset = 2;
  while (offset + 4 < view.byteLength) {
    const marker = view.getUint16(offset);
    const size = view.getUint16(offset + 2);
    if (marker === 0xffe1) {
      const exifStart = offset + 4;
      if (view.getUint32(exifStart) !== 0x45786966) { return 1; }
      const tiff = exifStart + 6;
      const little = view.getUint16(tiff) === 0x4949;
      const dirOffset = tiff + view.getUint32(tiff + 4, little);
      const entries = view.getUint16(dirOffset, little);
      for (let i = 0; i < entries; i++) {
        const entry = dirOffset + 2 + (i * 12);
        if (view.getUint16(entry, little) === 0x0112) {
          return view.getUint16(entry + 8, little);
        }
      }
      return 1;
    }
    if ((marker & 0xff00) !== 0xff00) { break; }
    offset += 2 + size;
  }

  return 1;
}

// EXIF orientation 값에 맞춰 캔버스를 회전/반전시킨다 (5~8은 가로세로가 뒤바뀐다).
function applyOrientation(context, orientation, width, height) {
  switch (orientation) {
    case 2: context.transform(-1, 0, 0, 1, width, 0); break;
    case 3: context.transform(-1, 0, 0, -1, width, height); break;
    case 4: context.transform(1, 0, 0, -1, 0, height); break;
    case 5: context.transform(0, 1, 1, 0, 0, 0); break;
    case 6: context.transform(0, 1, -1, 0, height, 0); break;
    case 7: context.transform(0, -1, -1, 0, height, width); break;
    case 8: context.transform(0, -1, 1, 0, 0, width); break;
    default: break;
  }
}

const isSwapped = orientation => orientation >= 5 && orientation <= 8;

// 숨긴 file input을 연다 — 모바일에서는 OS 시트가 그대로 뜬다(자체 시트 금지).
export function openPicker(input) {
  if (input) {
    input.value = '';   // 같은 파일을 다시 골라도 change가 발생하도록
    input.click();
  }
}

// input에 담긴 파일을 그대로 처리한다. Blazor로 바이트를 옮기지 않는다(큰 파일 마샬링 회피).
export async function prepareFromInput(input) {
  const file = input?.files?.[0];
  if (!file) {
    return { error: '파일을 읽지 못했어요', hint: '다시 선택해 주세요' };
  }

  const result = await prepare(file);
  return { ...result, fileName: file.name };
}

/**
 * 파일을 검사하고 리사이즈·회전 보정한 뒤 미리보기 URL과 크기를 돌려준다.
 * 실패는 예외가 아니라 { error } 로 반환한다 — 호출부가 인라인 카드로 보여줄 수 있게.
 */
export async function prepare(file) {
  if (!file) {
    return { error: '파일을 읽지 못했어요', hint: '다시 선택해 주세요' };
  }

  if (!ALLOWED.includes(file.type)) {
    return { error: '지원하지 않는 형식이에요', hint: 'jpg · png · webp 파일로 올려 주세요' };
  }

  if (file.size > MAX_BYTES) {
    const mb = (file.size / 1024 / 1024).toFixed(1);
    return { error: '업로드하지 못했어요', hint: `파일이 ${mb}MB예요 — 10MB 이하로 줄여 주세요` };
  }

  let loaded;
  try {
    loaded = await loadOrientedBitmap(file);
  } catch {
    return { error: '이미지를 열지 못했어요', hint: '손상된 파일일 수 있어요. 다른 사진을 선택해 주세요' };
  }

  const { bitmap, orientation } = loaded;
  const rawWidth = bitmap.width;
  const rawHeight = bitmap.height;
  const width = isSwapped(orientation) ? rawHeight : rawWidth;
  const height = isSwapped(orientation) ? rawWidth : rawHeight;

  const scale = Math.min(1, MAX_EDGE / Math.max(width, height));
  const targetWidth = Math.round(width * scale);
  const targetHeight = Math.round(height * scale);

  const canvas = document.createElement('canvas');
  canvas.width = targetWidth;
  canvas.height = targetHeight;
  const context = canvas.getContext('2d');
  applyOrientation(context, orientation, targetWidth, targetHeight);
  context.drawImage(bitmap, 0, 0, isSwapped(orientation) ? targetHeight : targetWidth,
    isSwapped(orientation) ? targetWidth : targetHeight);

  return {
    previewUrl: canvas.toDataURL('image/jpeg', JPEG_QUALITY),
    width: targetWidth,
    height: targetHeight,
    orientation,
  };
}

/**
 * 미리보기(dataURL)에서 지정 영역을 잘라 업로드한다.
 * crop: { x, y, width, height } — 원본 픽셀 기준. null이면 전체.
 */
export async function cropAndUpload(previewUrl, crop, outWidth, outHeight, category, token) {
  const image = await new Promise((resolve, reject) => {
    const element = new Image();
    element.onload = () => resolve(element);
    element.onerror = () => reject(new Error('preview decode failed'));
    element.src = previewUrl;
  });

  const area = crop ?? { x: 0, y: 0, width: image.width, height: image.height };
  const canvas = document.createElement('canvas');
  canvas.width = outWidth;
  canvas.height = outHeight;
  const context = canvas.getContext('2d');
  context.drawImage(image, area.x, area.y, area.width, area.height, 0, 0, outWidth, outHeight);

  const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', JPEG_QUALITY));
  if (!blob) {
    return { error: '이미지를 만들지 못했어요', hint: '다시 시도해 주세요' };
  }

  const form = new FormData();
  form.append('file', blob, 'upload.jpg');

  try {
    const response = await fetch(`/api/soccer/images/${category}`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    });

    const payload = await response.json();
    if (!response.ok || !payload?.isSuccess) {
      return { error: '업로드하지 못했어요', hint: '잠시 후 다시 시도해 주세요' };
    }

    return { url: payload.data.url };
  } catch {
    return { error: '업로드하지 못했어요', hint: '네트워크를 확인하고 다시 시도해 주세요' };
  }
}
