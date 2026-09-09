const PLAYER_NAMES = ["Bot 1", "Bot 2", "Bot 3", "Bot 4", "Bot 5"];
const SESSION_SECONDS = 60;

// Data/model: no DOM access, so the game rules stay easy to test or reuse.
export function createInitialState() {
  return {
    status: "waiting",
    remainingSeconds: SESSION_SECONDS,
    catches: [],
    totals: PLAYER_NAMES.map((name) => ({ name, totalWeight: 0, catches: 0 })),
    heaviest: null
  };
}

export function createCatch(random = Math.random) {
  const playerIndex = Math.floor(random() * PLAYER_NAMES.length);
  const weight = Math.round((0.5 + random() * 9.5) * 10) / 10;
  return { player: PLAYER_NAMES[playerIndex], weight, timestamp: Date.now() };
}

export function applyCatch(state, caught) {
  const next = structuredClone(state);
  const player = next.totals.find((entry) => entry.name === caught.player);
  player.totalWeight = Math.round((player.totalWeight + caught.weight) * 10) / 10;
  player.catches += 1;
  next.catches.push(caught);
  if (!next.heaviest || caught.weight > next.heaviest.weight) next.heaviest = caught;
  return next;
}

// Render/UI layer: only this section reads or writes the page.
const elements = {
  timer: document.querySelector("#timer"),
  status: document.querySelector("#session-status"),
  leaderboard: document.querySelector("#leaderboard"),
  heaviest: document.querySelector("#heaviest-catch"),
  toast: document.querySelector("#toast"),
  catchCount: document.querySelector("#catch-count"),
  start: document.querySelector("#start-button"),
  reset: document.querySelector("#reset-button")
};

let state = createInitialState();
let timerId = null;
let catchTimeoutId = null;
let toastTimeoutId = null;

function render() {
  const minutes = Math.floor(state.remainingSeconds / 60);
  const seconds = state.remainingSeconds % 60;
  elements.timer.textContent = `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  elements.status.textContent = state.status === "running" ? "Sesi Berjalan" : state.status === "ended" ? "Sesi Berakhir" : "Menunggu dimulai";
  elements.status.className = `status ${state.status}`;
  elements.catchCount.textContent = `${state.catches.length} tangkapan`;
  elements.start.disabled = state.status === "running";
  elements.start.textContent = state.status === "ended" ? "Mulai Lagi" : "Mulai Sesi";

  const sorted = [...state.totals].sort((a, b) => b.totalWeight - a.totalWeight);
  elements.leaderboard.innerHTML = sorted.map((player, index) => `
    <li><span class="rank">#${index + 1}</span><span class="player-name">${player.name} <small>(${player.catches} ikan)</small></span><span class="fish-total">${player.totalWeight.toFixed(1)} kg</span></li>
  `).join("");

  if (state.heaviest) {
    elements.heaviest.className = "heaviest-catch";
    elements.heaviest.innerHTML = `<span>${state.heaviest.player}</span><strong>${state.heaviest.weight.toFixed(1)} kg</strong><span>Tangkapan terbesar sesi</span>`;
  } else {
    elements.heaviest.className = "heaviest-empty";
    elements.heaviest.textContent = "Belum ada tangkapan.";
  }
}

function showCatchNotification(caught) {
  elements.toast.textContent = `${caught.player} dapat ikan ${caught.weight.toFixed(1)} kg!`;
  elements.toast.className = `toast visible${caught.weight > 7 ? " big-catch" : ""}`;
  if (caught.weight > 7) elements.heaviest.classList.add("flash");
  clearTimeout(toastTimeoutId);
  toastTimeoutId = setTimeout(() => elements.toast.classList.remove("visible"), 2000);
}

function scheduleCatch() {
  if (state.status !== "running") return;
  const delay = 3000 + Math.random() * 3000;
  catchTimeoutId = setTimeout(() => {
    if (state.status !== "running") return;
    const caught = createCatch();
    state = applyCatch(state, caught);
    render();
    showCatchNotification(caught);
    scheduleCatch();
  }, delay);
}

function endSession() {
  state = { ...state, status: "ended", remainingSeconds: 0 };
  clearInterval(timerId);
  clearTimeout(catchTimeoutId);
  render();
}

function startSession() {
  if (state.status === "ended") state = createInitialState();
  state = { ...state, status: "running" };
  clearInterval(timerId);
  timerId = setInterval(() => {
    state = { ...state, remainingSeconds: Math.max(0, state.remainingSeconds - 1) };
    render();
    if (state.remainingSeconds === 0) endSession();
  }, 1000);
  scheduleCatch();
  render();
}

elements.start.addEventListener("click", startSession);
elements.reset.addEventListener("click", () => {
  clearInterval(timerId);
  clearTimeout(catchTimeoutId);
  state = createInitialState();
  render();
});
render();
