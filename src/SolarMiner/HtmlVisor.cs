namespace SolarMiner;

/// Construye la pagina HTML interactivo de la cadena. El HTML lleva los datos de la
/// cadena embebidos y, en el navegador, recalcula los hashes con SHA-256 (la misma
/// función que usa C#) para verificar en vivo si la cadena está intacta o alterada.
public static class HtmlVisor
{
    public static string Build(string chainJson)
    {
        // El JSON de la cadena se inyecta tal cual dentro de una etiqueta script.
        return HtmlTemplate.Replace("/*__CHAIN_DATA__*/", chainJson);
    }

    private const string HtmlTemplate = """
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Visor de la Blockchain — Cooperativa de Energía Solar</title>
<style>
  :root {
    --bg: #0f1720; --panel: #16212e; --line: #24303f;
    --text: #e6edf3; --muted: #8b98a5;
    --ok: #2ec07a; --okbg: #10261c; --bad: #ff5c5c; --badbg: #2a1416;
    --accent: #4aa3ff; --amber: #e0a33e;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0; background: var(--bg); color: var(--text);
    font-family: -apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif;
    line-height: 1.5; padding: 32px 20px 80px;
  }
  .wrap { max-width: 920px; margin: 0 auto; }
  h1 { font-size: 26px; margin: 0 0 4px; }
  .sub { color: var(--muted); margin: 0 0 24px; font-size: 15px; }
  .status {
    padding: 14px 18px; border-radius: 10px; font-weight: 600;
    margin-bottom: 24px; border: 1px solid transparent; font-size: 15px;
  }
  .status.ok  { background: var(--okbg);  color: var(--ok);  border-color: #1c4a34; }
  .status.bad { background: var(--badbg); color: var(--bad); border-color: #5a2226; }
  .block {
    background: var(--panel); border: 1px solid var(--line);
    border-radius: 12px; padding: 18px 20px; margin-bottom: 18px;
    border-left: 5px solid var(--ok);
  }
  .block.invalid { border-left-color: var(--bad); background: #1a1416; }
  .block-head {
    display: flex; justify-content: space-between; align-items: center;
    margin-bottom: 12px; gap: 12px; flex-wrap: wrap;
  }
  .block-title { font-size: 17px; font-weight: 700; }
  .badge {
    font-size: 12px; font-weight: 700; padding: 4px 10px; border-radius: 999px;
    text-transform: uppercase; letter-spacing: .04em;
  }
  .badge.ok  { background: var(--okbg);  color: var(--ok); }
  .badge.bad { background: var(--badbg); color: var(--bad); }
  .field { margin: 8px 0; font-size: 14px; }
  .label { color: var(--muted); display: inline-block; min-width: 130px; vertical-align: top; }
  .mono { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 13px; word-break: break-all; }
  .hash-ok  { color: var(--ok); }
  .hash-bad { color: var(--bad); }
  .prev { color: var(--muted); }
  .tx-list { margin: 6px 0 0; max-height: 260px; overflow-y: auto; padding-right: 6px; }
  .tx-row { display: flex; align-items: center; gap: 8px; margin: 5px 0; }
  .tx-input {
    flex: 1; background: #0d1620; border: 1px solid var(--line); color: var(--text);
    border-radius: 6px; padding: 6px 9px; font-family: ui-monospace, Menlo, Consolas, monospace;
    font-size: 13px;
  }
  .tx-input:focus { outline: none; border-color: var(--accent); }
  .hint {
    background: #12202e; border: 1px solid var(--line); border-radius: 10px;
    padding: 14px 18px; margin-bottom: 24px; color: var(--muted); font-size: 14px;
  }
  .hint b { color: var(--amber); }
  .toolbar { margin-bottom: 20px; display: flex; gap: 10px; flex-wrap: wrap; }
  button {
    background: var(--accent); color: #04223f; border: none; border-radius: 8px;
    padding: 9px 16px; font-weight: 700; cursor: pointer; font-size: 14px;
  }
  button.ghost { background: transparent; color: var(--accent); border: 1px solid var(--accent); }
  button:hover { filter: brightness(1.08); }
  .chip { display:inline-block; background:#0d1620; border:1px solid var(--line);
    border-radius:6px; padding:2px 8px; font-size:12px; color:var(--muted); }
</style>
</head>
<body>
<div class="wrap">
  <h1>Blockchain de la Cooperativa de Energía Solar</h1>
  <p class="sub">Registro sellado de intercambios de energía · Visor de integridad en vivo</p>

  <div class="hint">
    <b>Cómo probar la inmutabilidad:</b> edita el valor de cualquier transacción (por
    ejemplo, cambia los kWh de un bloque). Al soltar, el navegador recalcula los hashes
    con la misma fórmula que usó el programa. Verás cómo el bloque alterado y
    <b>todos los siguientes</b> se marcan en rojo: la cadena se rompe. Así se demuestra
    que nadie puede cambiar un dato sellado sin que se detecte.
  </div>

  <div class="toolbar">
    <button class="ghost" onclick="resetChain()">Restaurar datos originales</button>
    <span class="chip" id="difficultyChip"></span>
  </div>

  <div id="status" class="status ok">Verificando…</div>
  <div id="chain"></div>
</div>

<script>
// ====== Datos de la cadena, generados por el programa C# ======
const CHAIN = /*__CHAIN_DATA__*/;

// Copia editable (para poder restaurar)
let workingBlocks = JSON.parse(JSON.stringify(CHAIN.blocks));

// ====== SHA-256 con la MISMA fórmula de contenido que C# ======
// contenido = index|timestamp|previousHash|tx1;tx2;...;|nonce
function blockContent(block, prevHashLive) {
  let s = block.index + "|" + block.timestamp + "|" + prevHashLive + "|";
  for (const tx of block.transactions) s += tx + ";";
  s += "|" + block.nonce;
  return s;
}

async function sha256hex(str) {
  const data = new TextEncoder().encode(str);
  const buf = await crypto.subtle.digest("SHA-256", data);
  return [...new Uint8Array(buf)].map(b => b.toString(16).padStart(2, "0")).join("");
}

function meetsDifficulty(hash, difficulty) {
  return hash.startsWith("0".repeat(difficulty));
}

// ====== Recalcula toda la cadena y pinta el estado ======
async function validateAndRender() {
  const difficulty = CHAIN.difficulty;
  let prevHash = CHAIN.genesisPreviousHash;
  let firstBroken = -1;
  const liveHashes = [];

  for (let i = 0; i < workingBlocks.length; i++) {
    const b = workingBlocks[i];
    const content = blockContent(b, prevHash);
    const liveHash = await sha256hex(content);
    liveHashes.push(liveHash);

    const valid = meetsDifficulty(liveHash, difficulty);
    if (!valid && firstBroken === -1) firstBroken = i;

    prevHash = liveHash; // el siguiente bloque se encadena a ESTE hash recalculado
  }

  render(liveHashes, firstBroken);
}

function render(liveHashes, firstBroken) {
  const difficulty = CHAIN.difficulty;
  const statusEl = document.getElementById("status");

  if (firstBroken === -1) {
    statusEl.className = "status ok";
    statusEl.textContent = "Cadena íntegra: todos los bloques están correctamente sellados y encadenados.";
  } else {
    statusEl.className = "status bad";
    statusEl.textContent = "!!Cadena rota a partir del bloque #" + firstBroken +
      ". Un dato fue alterado y el sello ya no coincide.";
  }

  const chainEl = document.getElementById("chain");
  chainEl.innerHTML = "";

  let prevShown = CHAIN.genesisPreviousHash;
  for (let i = 0; i < workingBlocks.length; i++) {
    const b = workingBlocks[i];
    const liveHash = liveHashes[i];
    const broken = !meetsDifficulty(liveHash, difficulty);

    const div = document.createElement("div");
    div.className = "block" + (broken ? " invalid" : "");

    const txRows = b.transactions.map((tx, j) =>
      `<div class="tx-row">
         <input class="tx-input" value="${escapeAttr(tx)}"
                oninput="onEdit(${i}, ${j}, this.value)">
       </div>`).join("");

    div.innerHTML = `
      <div class="block-head">
        <div class="block-title">${i === 0 ? "Bloque Génesis" : "Bloque #" + i}</div>
        <div class="badge ${broken ? "bad" : "ok"}">${broken ? "Alterado" : "Válido"}</div>
      </div>
      <div class="field"><span class="label">Transacciones</span>
        <div class="tx-list" style="display:inline-block; width:calc(100% - 140px);">${txRows}</div>
      </div>
      <div class="field"><span class="label">Hash anterior</span>
        <span class="mono prev">${prevShown}</span></div>
      <div class="field"><span class="label">Nonce (sello)</span>
        <span class="mono">${b.nonce}</span></div>
      <div class="field"><span class="label">Hash de este bloque</span>
        <span class="mono ${broken ? "hash-bad" : "hash-ok"}">${liveHash}</span></div>
    `;
    chainEl.appendChild(div);
    prevShown = liveHash;
  }

  document.getElementById("difficultyChip").textContent =
    "Dificultad: " + difficulty + " ceros";
}

function onEdit(blockIndex, txIndex, value) {
  workingBlocks[blockIndex].transactions[txIndex] = value;
  validateAndRender();
}

function resetChain() {
  workingBlocks = JSON.parse(JSON.stringify(CHAIN.blocks));
  validateAndRender();
}

function escapeAttr(s) {
  return String(s).replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
}

validateAndRender();
</script>
</body>
</html>
""";
}
