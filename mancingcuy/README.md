# MancingCuy — Fishing Pond Tournament

Game memancing bergaya empang Indonesia dengan sistem turnamen Galatama. Dibuat di Unity 6 (WebGL).

## Cara Menjalankan

1. Buka project di **Unity 6+**
2. Menu **Mancing Cuy → Generate Fishing Pond Scene** — membuat seluruh scene secara prosedural
3. Buka scene `Assets/FishingPondGenerated/FishingPond.unity`
4. Tekan **Play**

### Build & Deploy (WebGL)

1. **File → Build Profiles** → Web Desktop Development → Build ke folder `WebGLBuild` atau `WebGLBuildNew`
2. Jalankan `deploy-webgl.bat` — otomatis copy ke `docs/`, commit, push
3. GitHub Actions / Vercel deploy dari folder `docs/`

## Keputusan Desain

- **Scene prosedural** — seluruh scene (terrain, kolam, pohon, NPC, kamera) di-generate dari `Editor/FishingPondGenerator.cs` via `[MenuItem]`. Tidak ada prefab scene manual; satu script = satu sumber kebenaran.
- **State machine eksplisit** — Player dan bot fishing pakai `enum FishingState` (Idle → ReadyToCast → Casting → WaitingForBite → Pulling → ReelBack). Menghindari spaghetti boolean flag.
- **IK untuk rod grip** — `OnAnimatorIK` + `SetIKPosition/Rotation` agar tangan kiri pegang joran natural, tanpa custom animation retargeting.
- **Bone parenting** — Joran di-parent ke `HumanBodyBones.RightHand` saat runtime. Kompatibel dengan karakter Humanoid manapun.
- **Fishing line prosedural** — Cylinder di-scale dan di-rotate dari rod tip ke bobber/ikan. Tidak pakai LineRenderer (overkill untuk satu garis lurus).
- **Clip duration caching** — Durasi animasi di-cache dari `runtimeAnimatorController.animationClips` saat Start, sinkronisasi state transition ke panjang clip sebenarnya.
- **Mobile support via OnGUI** — Virtual joystick + tombol aksi di-render dengan IMGUI. Auto-detect mobile (touch + layar kecil). Desktop tetap keyboard. Tidak ada dependency tambahan.
- **IMGUI untuk semua UI** — Leaderboard, minigame, inventory, toast semua pakai `OnGUI`. Zero dependency pada UI Toolkit/Canvas. Trade-off: kurang cantik, tapi zero-setup dan bisa di-generate prosedural.
- **Bot AI sederhana** — Bot jalan ke spot, hadap kolam, cast, tunggu. Catch di-trigger oleh `FishingLeaderboardController` dengan random timer. Tidak ada pathfinding kompleks — cukup `CharacterController.Move` ke titik target.
- **Invisible pond barriers** — BoxCollider tanpa Renderer di sekeliling kolam. Lebih murah dan predictable daripada mesh collider atau NavMesh.

## Yang Bisa Diperbaiki

- **UI**: Migrasi dari IMGUI ke UI Toolkit atau uGUI Canvas — scaling, theming, dan responsivitas jauh lebih baik.
- **Ukuran build**: `.wasm` 90MB karena Development build tanpa stripping. Release build + kompresi Brotli bisa turun ke ~15-20MB.
- **Fish variety**: Saat ini semua ikan pakai prefab yang sama. Bisa ditambah variasi model, ukuran, dan raritas per spesies.
- **Sound & VFX**: Tidak ada audio atau efek partikel (splash, casting swoosh). Menambah immersion signifikan.
- **Bot AI**: Bot hanya berdiri diam memancing. Bisa ditambah animasi idle variation, walk-around antar spot, dan difficulty scaling.
- **Minigame depth**: Hanya satu timing bar. Bisa ditambah tension meter, reel mechanic, atau fish fighting pattern berdasarkan berat ikan.
- **Multiplayer**: Arsitektur saat ini single-player. WebSocket + lobby bisa mengubah bot jadi pemain nyata.
- **Save system**: Tidak ada persistensi. `PlayerPrefs` atau IndexedDB (WebGL) bisa menyimpan highscore dan inventori lintas sesi.
- **Camera**: Fixed follow camera. Bisa ditambah orbit/zoom dan cutscene saat menarik ikan besar.
- **Accessibility**: Tidak ada keybinding remap, colorblind mode, atau screen reader support.
