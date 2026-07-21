// 선수 카드 공용 — Design.PlayerPublicProfile 카드 뷰.
// 화면 카드는 HTML이 그리고, 여기는 ① QR 렌더 ② 1080×1350 PNG 저장(캔버스 직접 렌더) ③ 링크 복사만 맡는다.
// html2canvas류를 쓰지 않는 이유: 카드 레이아웃이 고정이라 직접 그리는 쪽이 결정적이고 의존성이 없다.
import qrcode from './qrcode.vendor.js';

//.// QR — 화면 표시용 (흰 박스 안에 셀을 직접 그린다)

export function renderQr(canvas, text) {
    const qr = qrcode(0, 'M');
    qr.addData(text);
    qr.make();

    const count = qr.getModuleCount();
    const size = canvas.width; // 정사각 가정
    const cell = size / count;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, size, size);
    ctx.fillStyle = '#1c2b4a';
    for (let r = 0; r < count; r++) {
        for (let c = 0; c < count; c++) {
            if (qr.isDark(r, c)) {
                ctx.fillRect(Math.floor(c * cell), Math.floor(r * cell), Math.ceil(cell), Math.ceil(cell));
            }
        }
    }
}

//.// 링크 복사

export async function copyLink(url) {
    try {
        await navigator.clipboard.writeText(url);
        return true;
    } catch {
        return false;
    }
}

//.// 이미지 저장 — dc 카드(360px) 좌표 × 3 = 1080×1350

const S = 3; // 스케일

function roundRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
}

function loadImage(url) {
    return new Promise(resolve => {
        if (!url) { resolve(null); return; }
        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = () => resolve(img);
        img.onerror = () => resolve(null); // 외부 이미지 CORS 실패 → 이니셜 폴백
        img.src = url;
    });
}

// data: { name, meta, teamName, seasonLabel, jersey, photoUrl, bodyLines[], stats[{value,label,teal}],
//         granted: { lines[] } | null, url, footerNote, caption, fileName }
export async function saveCardImage(data) {
    // 저장 크기는 1080×1350 고정 (SPEC — 4:5). 권한 블록 유무는 내부 여백으로 흡수한다.
    const width = 360 * S;
    const height = 450 * S;
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d');

    //.// 배경 그라디언트 + 장식 원 (radius 22)
    roundRect(ctx, 0, 0, width, height, 22 * S);
    ctx.save();
    ctx.clip();
    const bg = ctx.createLinearGradient(0, 0, width * 0.35, height);
    bg.addColorStop(0, '#1c2b4a');
    bg.addColorStop(1, '#23408e');
    ctx.fillStyle = bg;
    ctx.fillRect(0, 0, width, height);

    ctx.strokeStyle = 'rgba(255,255,255,.09)';
    ctx.lineWidth = 1.5 * S;
    ctx.beginPath();
    ctx.arc(width + 40 * S, -70 * S + 110 * S, 110 * S, 0, Math.PI * 2);
    ctx.stroke();
    ctx.strokeStyle = 'rgba(46,196,182,.25)';
    ctx.beginPath();
    ctx.arc(width + 35 * S, 95 * S, 75 * S, 0, Math.PI * 2);
    ctx.stroke();

    const font = (px, weight) => `${weight} ${px * S}px Pretendard, "Plus Jakarta Sans", sans-serif`;

    //.// 헤더 — 실드 + 팀명·시즌 + #등번호
    const hx = 24 * S, hy = 22 * S;
    ctx.fillStyle = 'rgba(255,255,255,.15)';
    roundRect(ctx, hx, hy, 22 * S, 25 * S, 6 * S);
    ctx.fill();
    ctx.fillStyle = 'rgba(46,196,182,.7)';
    roundRect(ctx, hx + 3 * S, hy + 4 * S, 16 * S, 17 * S, 4 * S);
    ctx.fill();

    ctx.fillStyle = '#ffffff';
    ctx.font = font(12, 800);
    ctx.textBaseline = 'top';
    ctx.fillText(data.teamName ?? '', hx + 31 * S, hy + 1 * S);
    ctx.fillStyle = 'rgba(255,255,255,.55)';
    ctx.font = font(10, 500);
    ctx.fillText(data.seasonLabel ?? '', hx + 31 * S, hy + 15 * S);

    if (data.jersey) {
        ctx.fillStyle = 'rgba(255,255,255,.25)';
        ctx.font = font(26, 800);
        ctx.textAlign = 'right';
        ctx.fillText(data.jersey, width - 24 * S, hy - 4 * S);
        ctx.textAlign = 'left';
    }

    //.// 사진(3:4) + 이름 블록
    const px = 24 * S, py = 66 * S, pw = 132 * S, ph = 176 * S;
    roundRect(ctx, px, py, pw, ph, 13 * S);
    ctx.save();
    ctx.clip();
    const photo = await loadImage(data.photoUrl);
    if (photo) {
        // object-cover — 중앙 크롭
        const scale = Math.max(pw / photo.width, ph / photo.height);
        const dw = photo.width * scale, dh = photo.height * scale;
        ctx.drawImage(photo, px + (pw - dw) / 2, py + (ph - dh) / 2, dw, dh);
    } else {
        ctx.fillStyle = 'rgba(255,255,255,.08)';
        ctx.fillRect(px, py, pw, ph);
        ctx.fillStyle = '#2EC4B6';
        ctx.beginPath();
        ctx.arc(px + pw / 2, py + ph / 2, 28 * S, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = '#ffffff';
        ctx.font = font(20, 800);
        ctx.textAlign = 'center';
        ctx.fillText((data.name ?? '?').slice(0, 1), px + pw / 2, py + ph / 2 - 10 * S);
        ctx.textAlign = 'left';
    }
    ctx.restore();
    ctx.strokeStyle = 'rgba(255,255,255,.15)';
    ctx.lineWidth = 1 * S;
    roundRect(ctx, px, py, pw, ph, 13 * S);
    ctx.stroke();

    const tx = px + pw + 16 * S;
    let ty = py + ph - 90 * S;
    ctx.fillStyle = '#ffffff';
    ctx.font = font(24, 800);
    ctx.fillText(data.name ?? '', tx, ty);
    ty += 32 * S;
    ctx.fillStyle = '#2EC4B6';
    ctx.font = font(12.5, 700);
    ctx.fillText(data.meta ?? '', tx, ty);
    ty += 20 * S;
    ctx.fillStyle = 'rgba(255,255,255,.6)';
    ctx.font = font(11, 500);
    for (const line of data.bodyLines ?? []) {
        ctx.fillText(line, tx, ty);
        ty += 16 * S;
    }

    //.// 스탯 칩 4개
    if (data.stats?.length) {
        const gap = 8 * S, sx0 = 24 * S, sy = 260 * S, sh = 52 * S;
        const sw = (width - 48 * S - gap * (data.stats.length - 1)) / data.stats.length;
        data.stats.forEach((stat, i) => {
            const sx = sx0 + i * (sw + gap);
            ctx.fillStyle = 'rgba(255,255,255,.08)';
            roundRect(ctx, sx, sy, sw, sh, 11 * S);
            ctx.fill();
            ctx.textAlign = 'center';
            ctx.fillStyle = stat.teal ? '#2EC4B6' : '#ffffff';
            ctx.font = font(17, 800);
            ctx.fillText(stat.value, sx + sw / 2, sy + 9 * S);
            ctx.fillStyle = 'rgba(255,255,255,.55)';
            ctx.font = font(9.5, 700);
            ctx.fillText(stat.label, sx + sw / 2, sy + 33 * S);
            ctx.textAlign = 'left';
        });
    }

    //.// 권한 블록 — 점선 상단 보더 + 승인 열람 정보 (하단 바 위에 붙인다)
    const barY = 390 * S;
    if (data.granted) {
        const gy = 318 * S;
        ctx.strokeStyle = 'rgba(255,255,255,.2)';
        ctx.lineWidth = 1 * S;
        ctx.setLineDash([4 * S, 4 * S]);
        ctx.beginPath();
        ctx.moveTo(24 * S, gy);
        ctx.lineTo(width - 24 * S, gy);
        ctx.stroke();
        ctx.setLineDash([]);

        ctx.fillStyle = '#2EC4B6';
        ctx.font = font(11, 700);
        ctx.fillText('승인 열람 정보', 24 * S, gy + 12 * S);
        ctx.fillStyle = 'rgba(255,255,255,.8)';
        ctx.font = font(12, 500);
        let ly = gy + 32 * S;
        for (const line of data.granted.lines ?? []) {
            ctx.fillText(line, 24 * S, ly);
            ly += 18 * S;
        }
    }

    //.// 하단 바 — QR + URL + 워터마크
    ctx.fillStyle = 'rgba(15,20,30,.35)';
    ctx.fillRect(0, barY, width, height - barY);

    const qy = barY + 13 * S;
    ctx.fillStyle = '#ffffff';
    roundRect(ctx, 24 * S, qy, 34 * S, 34 * S, 8 * S);
    ctx.fill();
    const qr = qrcode(0, 'M');
    qr.addData(data.url ?? '');
    qr.make();
    const count = qr.getModuleCount();
    const qsize = 28 * S, qcell = qsize / count;
    const qx0 = 24 * S + 3 * S, qy0 = qy + 3 * S;
    ctx.fillStyle = '#1c2b4a';
    for (let r = 0; r < count; r++) {
        for (let c = 0; c < count; c++) {
            if (qr.isDark(r, c)) {
                ctx.fillRect(qx0 + c * qcell, qy0 + r * qcell, Math.ceil(qcell), Math.ceil(qcell));
            }
        }
    }

    ctx.fillStyle = '#ffffff';
    ctx.font = font(11, 700);
    ctx.fillText(data.urlLabel ?? '', 67 * S, qy + 5 * S);
    ctx.fillStyle = 'rgba(255,255,255,.5)';
    ctx.font = font(10, 500);
    ctx.fillText(data.footerNote ?? '', 67 * S, qy + 20 * S);

    ctx.fillStyle = 'rgba(255,255,255,.4)';
    ctx.font = font(10, 700);
    ctx.textAlign = 'right';
    ctx.fillText('PlayGround', width - 24 * S, qy + 12 * S);
    ctx.textAlign = 'left';

    // 권한 카드 워터마크 — 재공유 억제 (dc 캡션 "워터마크 포함")
    if (data.granted) {
        ctx.save();
        ctx.translate(width / 2, height / 2);
        ctx.rotate(-Math.PI / 7);
        ctx.fillStyle = 'rgba(255,255,255,.07)';
        ctx.font = font(24, 800);
        ctx.textAlign = 'center';
        ctx.fillText('PlayGround · 재공유 금지', 0, 0);
        ctx.restore();
    }

    ctx.restore(); // 카드 radius 클리핑 해제

    //.// 다운로드 (1080×1350)
    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
    if (!blob) { return false; }
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = data.fileName ?? 'player-card.png';
    link.click();
    URL.revokeObjectURL(link.href);
    return true;
}
