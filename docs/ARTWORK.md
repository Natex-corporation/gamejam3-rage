# Clay interface artwork

The project-owned asset is [`assets/clay-ui.png`](../assets/clay-ui.png). It was generated with the built-in ImageGen tool, using the game's handmade clay village as visual reference. The PNG retains transparency and contains four equal cells: cream with ochre edging, butter yellow, coral, and green (top-left to bottom-right).

`ClayUI.cs` loads it once at runtime from `GameJam3_Rage_Data/Refinement/Assets/`, creates four sprites, and uses [Unity UI Toolkit nine-slice backgrounds](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-uss/properties/uib-styling-ui-backgrounds). The build script copies the asset into that location. The corners and edge shading remain fixed while the center stretches to each surface. All added buttons, panels, HUD badges, and localized captions share it.

## Generation prompt

```text
Use case: stylized-concept. Asset type: production game UI nine-slice sprite atlas. Create ONE transparent PNG sprite atlas, square 1024x1024, exact 2 by 2 grid, each cell 512x512. In EACH cell place one separate front-facing almost-square rounded slab of real hand-kneaded plasticine clay, occupying x/y from 24 to 488 within its cell. Top-left cell: light warm cream clay slab, broad empty center for dark text, thin ochre clay rim. Top-right cell: pale buttery ochre yellow clay slab. Bottom-left cell: light salmon coral red clay slab. Bottom-right cell: pale leafy green clay slab. All four slabs same silhouette: slightly irregular softly rounded corners, gently uneven pressed edges, handcrafted finger marks and fine clay grain concentrated around edges, restrained tactile surface, broad nearly uniform quiet central 70 percent for text. Soft photographic stop-motion clay lighting from upper left, subtle bottom-right edge depth and very soft contact shadow inside each cell, no big cast shadows. Match the handmade clay houses and figures in a children's clay animation game. These are nine-slice textures so edges should be simple continuous material and center mostly uniform. Orthographic straight-on view, no perspective. Absolutely NO text, NO letters, NO icons, NO decorations, NO borders between cells, NO opaque background, NO checkerboard. Leave 24 transparent pixels padding around each slab in its own cell; nothing crosses cell boundaries.
```

The returned atlas is 1254×1254; the loader divides its actual dimensions into equal cells rather than assuming the requested resolution. No font or original game art is embedded in it. All text remains live, localized UI text.

## Kiln King boss

`assets/kiln-king.png` is project-owned artwork generated with the built-in ImageGen tool. It retains transparency and is shared under MIT. `DemoContent.LoadSprite` uses a bottom-center pivot and scales the image to 4.8 world units high. Its original clay texture is preserved; the runtime adds a small stepped wobble and hit flash.

Exact generation prompt:

```text
Use case: stylized-concept. Asset type: single production 2D game boss sprite, transparent PNG. Create a full body front-facing stop-motion plasticine character called the Kiln King, for a handmade clay platformer with photographed clay houses and little clay figures. A chunky terracotta-orange kiln creature with a rounded pot-bellied body, stubby thick feet, broad soft mitten fists held low at his sides, a small lumpy three-point ochre crown made of clay, a warm glowing cream-orange furnace-mouth inset in his belly, two large expressive dark coal eyes with small cream glints and furrowed brows. Grumpy, humorous and formidable, suitable for a playful village game, no horror. Clearly hand kneaded colored plasticine with fingerprint texture, small dents and imperfect sculpted edges; soft studio lighting from upper left. Orthographic flat straight-on view, both feet on the same ground baseline near bottom, entire character including crown and hands visible with transparent margin on all sides, centered, fills 85 percent of canvas. Clearly separated silhouette for readable collision shape. No ground, no scenery, no text, no letters, no logo, no watermark, no checkerboard, no large cast shadow. One character only, one pose, no sprite sheet. Wide oval body and squat proportions, roughly 1.1 times as tall as wide.
```
