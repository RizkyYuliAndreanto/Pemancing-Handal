# MancingCuy — Pemancing Handal 🎣

Game memancing 3D bergaya empang Indonesia dengan turnamen **Galatama** melawan 4 bot. Dibuat dengan **Unity 6 (WebGL)**, bisa dimainkan langsung di browser **PC maupun HP** tanpa install apa pun.

> **▶️ MAIN SEKARANG: [https://pemancing-handal.vercel.app](https://pemancing-handal.vercel.app)**
>
> Buka di laptop (pakai keyboard + mouse) atau di HP (pakai joystick virtual + tombol sentuh). File `.wasm` ~90MB, jadi loading pertama butuh beberapa saat — ditampilkan progress bar Unity.

---

## Gameplay

1. Karakter jalan mendekati tepi kolam
2. Tekan **ENTER** (PC) / tombol **MULAI** (HP) untuk memulai sesi 60 detik
3. **Lempar pancing** — pelampung melayang ke titik acak di kolam (bisa sampai tengah)
4. Tunggu ikan menyentak, lalu menangkan **minigame timing**: tekan SPACE/TARIK saat marker bergerak berada di **zona hijau**
5. Ikan tertarik masuk ke inventori, bobot tercatat di leaderboard
6. Setelah menarik, pemancing kembali ke posisi **siap lempar** — menunggu input pemain (tidak auto-cast)
7. Kumpulkan total berat terbesar; bersaing dengan Bot_A–Bot_D yang juga memancing secara live

---

## Kontrol

### 💻 PC / Desktop

| Aksi | Tombol |
|---|---|
| Gerak | `W A S D` / Arrow Keys |
| Kamera | Gerakkan mouse (cursor ter-lock; `ESC` untuk lepas, klik untuk lock lagi) |
| Mulai sesi | `ENTER` |
| Lempar pancing | Klik tombol **LEMPAR PANCING** / `ENTER` / klik kiri |
| Menarik ikan (minigame) | `SPACE` saat marker di zona hijau |

### 📱 Mobile / HP

| Aksi | Kontrol |
|---|---|
| Gerak | **Joystick virtual** (kiri bawah — sentuh & drag di area kiri bawah layar) |
| Kamera | **Drag** di area layar mana pun selain joystick/tombol |
| Mulai sesi | Tombol **MULAI** (kanan bawah) |
| Lempar pancing | Tombol **LEMPAR** |
| Menarik ikan | Tombol **TARIK** saat marker di zona hijau |

Mode mobile aktif otomatis — tidak ada tombol untuk dipilih. Deteksi berdasarkan dukungan touch + lebar layar.

---

## Cara Menjalankan Project

### Prasyarat

- **Unity 6+** (pakai *Build Profiles*, bukan Build Settings lama)
- Asset pihak ketiga yang di-import (sudah ada di repo):
  - *Kevin Iglesias — Human Animations* (klip animasi memancing + joran)
  - *Alstra Infinite — Fish PolyPack* (prefab ikan)
  - *Floreswa* (prefab karakter humanoid)

### Menjalankan di Editor

1. Buka folder `mancingcuy/` sebagai project di Unity Hub
2. Buka scene `Assets/FishingPondGenerated/FishingPond.unity`
3. **(Opsional)** Jika ingin regenerate scene dari nol: menu **Mancing Cuy → Generate Fishing Pond Scene** — seluruh dunia (terrain, kolam, hiasan, 4 bot, player, kamera) dibangun ulang secara prosedural
4. Tekan **Play**

### Build WebGL

1. **File → Build Profiles** → pilih profil **Web → Desktop → Development**
2. Klik **Build** → arahkan output ke folder `WebGLBuild` (atau `WebGLBuildNew`) di root project
3. Build menghasilkan `index.html`, `Build/*.wasm`, `Build/*.data`, `Build/*.framework.js`, dan `TemplateData/`

### Deploy

Jalankan `deploy-webgl.bat` dari root project (double-click). Script otomatis:

1. Memvalidasi build WebGL ada
2. Menghapus & menyalin ulang isi build ke folder `docs/`
3. `git add docs/` → commit → push ke `main`

Dua jalur hosting yang sudah dikonfigurasi:

- **GitHub Pages** — workflow `.github/workflows/deploy-webgl.yml` menjalankan `actions/deploy-pages` dari folder `docs/` setiap push yang menyentuh `docs/**`
- **Vercel** — project terhubung ke repo GitHub, *Root Directory* diarahkan ke `mancingcuy/docs`, tanpa build command. Setiap push ke `main` → auto redeploy → live di `pemancing-handal.vercel.app`

---

## Struktur Kode Penting

| File | Peran |
|---|---|
| `Assets/Editor/FishingPondGenerator.cs` | Generator scene prosedural via `[MenuItem]` — satu sumber kebenaran untuk seluruh isi dunia |
| `Assets/FishingPondGenerated/FishingPlayerController.cs` | State machine memancing pemain: gerak (keyboard + joystick), cast, IK, animasi, senar & bobber |
| `Assets/FishingPondGenerated/FishingBot.cs` | Versi bot dari controller yang sama — jalan ke spot, hadap kolam, cast, siklus lempar-ulang |
| `Assets/FishingPondGenerated/FishingLeaderboardController.cs` | Otak turnamen: timer 60 detik, trigger gigitan pemain, minigame timing bar, simulate catch bot, seluruh UI (OnGUI) |
| `Assets/FishingPondGenerated/FishingThirdPersonCamera.cs` | Kamera orbit third-person — mouse lock di PC, touch drag di mobile |
| `Assets/FishingPondGenerated/MobileTouchControls.cs` | Joystick virtual + tombol aksi + kamera drag untuk mobile; menyediakan state input static yang dibaca script lain |
| `Assets/FishingPondGenerated/FishingLocomotionAnimator.cs` | Animasi jalan prosedural (rotasi bone via LateUpdate) saat tidak memancing |
| `.github/workflows/deploy-webgl.yml` | GitHub Actions: deploy `docs/` ke GitHub Pages |
| `deploy-webgl.bat` | Otomasi build-to-deploy untuk Windows |

---

## Keputusan Desain

**1. Scene 100% prosedural (bukan scene manual).**
Satu editor script membangun seluruh dunia: terrain, kolam empang, tanggul beton, warung tarpaulin, saung atap alang-alang, pohon kelapa, batu, bangku, sign, hingga penempatan karakter. Keputusan ini mengambil trade-off: setup awal lebih lama daripada drag-and-drop di Scene view, tetapi setiap perubahan layout cukup edit satu file dan regenerate — tidak ada drift antara "scene di editor" dan "scene di repo". Semua material dibuat runtime sehingga repo bebas asset biner buatan sendiri.

**2. State machine enum eksplisit untuk alur memancing.**
`FishingState { Idle → ReadyToCast → Casting → WaitingForBite → Pulling → ReelBack }` dipakai identik oleh player dan bot. Versi awal bot memakai boolean flag (`lineInWater`, `isCasting`, ...) yang menghasilkan dead-code: kondisi transisi tidak pernah terpenuhi karena berada di dalam blok yang menuntut kondisi kebalikannya. Enum memaksa semua transisi terlihat di satu `switch` dan mencegah kombinasi state ilegal.

**3. Joran di-parent ke bone tangan + IK untuk tangan kiri.**
Rod menjadi child `HumanBodyBones.RightHand` saat runtime, tangan kiri ditarik ke grip depan via `OnAnimatorIK` (`SetIKPosition/Rotation`). Konsekuensinya karakter humanoid apa pun (Floreswa, Kevin Iglesias, dll) bisa pegang joran tanpa custom retarget — hanya butuh Avatar Humanoid yang valid.

**4. Senar & bobber prosedural, bukan LineRenderer.**
Senar = cylinder yang di-scale/rotate tiap frame dari rod tip ke bobber/ikan. Untuk satu garis lurus, LineRenderer justru overkill; pendekatan ini juga meniru persis konsep yang diminta ("senar harus dari ujung joran").

**5. Sinkronisasi animasi ke durasi clip asli + `HoldAnimation()` pengunci state.**
Durasi clip di-cache dari `runtimeAnimatorController.animationClips` saat Start, sehingga timer state (`clipCastDuration`, `clipFightDuration`) selalu cocok dengan animasi yang tampil. Bug penting yang ditemukan: Animator Controller bawaan (Kevin Iglesias) memiliki **auto-transition `HasExitTime` tanpa kondisi** yang siklus Begin→Loop→Fighting→Stop→Begin terus-menerus — akibatnya animasi lempar muncul saat harusnya menunggu/menarik. Solusi: `HoldAnimation()` membandingkan state aktif animator dengan state yang seharusnya tiap frame, dan memaksa `Play()` kembali jika animator "kabur" sendiri.

**6. Auto-cast dihilangkan — kembali ke `ReadyToCast` setelah ReelBack.**
Awalnya setelah menarik ikan, karakter langsung melempar lagi tanpa izin. Sekarang ReelBack berakhir di `ReadyToCast` dan menunggu input (ENTER/klik/tombol LEMPAR) — sesuai ekspektasi pemain.

**7. IMGUI (OnGUI) untuk seluruh UI.**
Leaderboard, inventori, toast, minigame bar, panel bantuan, hingga kontrol mobile semuanya IMGUI. Trade-off yang sadar: tampilan tidak sebagus uGUI/UI Toolkit, tetapi zero-setup (tanpa Canvas/prefab UI), bisa di-scale dengan satu faktor `s`, dan konsisten dengan pendekatan "semuanya dari kode".

**8. Mobile support sebagai static input layer, bukan komponen terpisah per objek.**
`MobileTouchControls` mengekspos `JoystickInput`, `EnterPressed/SpacePressed/CastPressed`, `CameraDelta`, dan `IsMobile` sebagai properti static. Controller kamera/player/leaderboard cukup membaca statik itu — tanpa referensi antar-komponen. Detail teknis yang menentukan:

- **Urutan eksekusi OnGUI vs Update**: `OnGUI` berjalan *setelah* `Update` dan bisa terpanggil berkali-kali per frame. Flag dari `GUI.Button` yang langsung di-clear di `Update` berikutnya sering terlewat — itulah sebabnya tombol mobile "tidak bisa ditekan" pada versi pertama. Perbaikannya: flag di-*persist* hingga `Update` membacanya satu kali (set → expose 1 frame → clear).
- **Manajemen finger ID**: joystick, kamera drag, dan tombol berbagi layar. Touch yang jatuh di dalam rect tombol diabaikan oleh pengambilan joystick/kamera agar tidak berebut finger; sisa layar → joystick (kiri-bawah) atau kamera drag (sisanya).
- **Kamera mobile**: cursor lock tidak berarti di HP, jadi kamera membaca delta touch drag; desktop tetap mouse-delta dengan cursor lock.
- **Skala UI**: semua rect dikalikan faktor `s = Screen.width / 800` di mode mobile.

**9. UI mobile-aware dan anti-overlap dengan dev console.**
Tulisan instruksi ("WASD menuju kolam..."), area minigame, dan status debug dinaikkan dari tepi bawah karena di WebGL bertumpuk dengan console log Unity (development build). Toast notif ("Bot_A dapat ikan 5.2 kg!") dipindah ke **atas tengah** agar selalu terbaca.

**10. Bot AI sederhana berbasis orkestrasi eksternal.**
Bot tidak punya logika "kapan dapat ikan" — `FishingLeaderboardController` memilih bot yang sedang `WaitingForBite` secara acak tiap 3–6 detik dan memanggil `TriggerCatch(weight)`. Manfaatnya: pacing turnamen (berapa sering bot dapat ikan, berapa berat) dikendalikan di satu tempat, dan bot tetap terlihat hidup karena animasi fighting/jaring benar-benar diputar.

**11. Deployment statis tanpa backend.**
`docs/` berisi build WebGL murni; GitHub Pages dan Vercel hanya menyajikan file statis. Tidak ada server, tidak ada biaya, dan rollback cukup `git revert`.

---

## Yang Masih Bisa Diperbaiki

**Prioritas tinggi**

- **Ukuran build** — `.wasm` ~90MB karena Development build tanpa kompresi. Release build + **Compression Format: Gzip/Brotli** (+ Decompression Fallback) + code stripping akan menurunkannya ke ~15–25MB. Ini perbaikan #1 untuk pengalaman mobile (loading pertama lama, dan menghabiskan kuota).
- **UI framework** — migrasi IMGUI → **UI Toolkit** atau uGUI Canvas. IMGUI sulit diberi animasi, tema, dan layout responsif yang benar; performa OnGUI juga menurun di layar banyak elemen.
- **Haptik & feedback mobile** — sentuhan tombol tidak memberi respon visual/haptic; `Handheld.Vibrate()` atau animasi tekan akan membantu.
- **Deteksi mobile** — heuristik `touchSupported && Screen.width < 1200` bisa salah: iPad landscape (>1200px) dianggap desktop, touchscreen laptop bisa dianggap mobile. Lebih tepat mengekspos toggle manual di layar.

**Prioritas menengah**

- **Variasi ikan** — satu prefab ikan untuk semua tangkapan. Menarik untuk: beberapa spesies (lele, nila, gurame, patin — khas empang), rentang berat per spesies, rarity, dan nilai skor berbeda.
- **Audio** — belum ada suhuara sama sekali: splash saat bobber jatuh, deritan reel saat fighting, ambient sawah, dan jingle saat menang minigame.
- **VFX** — partikel percikan air saat cast/strike, riak air di sekitar bobber, jejak senar melengkung saat fighting.
- **Kedalaman minigame** — timing bar tunggal terasa datar untuk ikan berat. Ide: tension meter (tarik terlalu cepat = senar putus), pola perlawanan berbeda per berat/spesies, atau QTE beruntun untuk ikan trofi.
- **Kamera** — tidak ada zoom, tidak ada collision dengan objek (kamera bisa menembus pohon/saung), tidak ada cutscene singkat saat dapat ikan besar.

**Prioritas rendah / jangka panjang**

- **Persistensi** — highscore dan rekorman tersimpan via `localStorage` (WebGL) agar pemain punya alasan kembali.
- **Multiplayer nyata** — arsitektur bot saat ini mudah "ditukar": ganti `FishingBot` dengan client remote via WebSocket (mis. server kecil + lobby). Leaderboard sudah abstrak per-index pemain.
- **Perilaku bot** — bot diam memancing di satu spot; variasi idle, pindah spot setelah beberapa tangkapan, dan reaksi saat tetangga dapat ikan akan membuat dunia terasa hidup.
- **Aksesibilitas** — remap tombol, mode buta warna (zona hijau minigame bergantung pada warna), opsi ukuran teks.
- **Lokalisasi** — teks campuran Indonesia/Inggris ("TARIK", "SESI BERJALAN"); satu sistem string akan memudahkan tambah bahasa.

---

## Repo & Link

- **Repository**: [github.com/RizkyYuliAndreanto/Pemancing-Handal](https://github.com/RizkyYuliAndreanto/Pemancing-Handal)
- **Demo live**: [https://pemancing-handal.vercel.app](https://pemancing-handal.vercel.app) (PC & mobile)
