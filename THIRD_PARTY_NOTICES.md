# Third-party notices and scope

The root MIT license covers the original refinement source, scripts, and documentation in this repository. It does not grant rights to external dependencies or relicense the original game.

| Material | Treatment |
| --- | --- |
| Unity player and managed libraries | Supplied by a separate game installation; not committed here. Governed by Unity's applicable terms. |
| Original game assembly, scenes, artwork, and audio | Not included in the source checkout. Obtain a compatible game separately. |
| StrawberryMuffins font | Reused from assets already loaded by the game. No font files are distributed here; its original license applies. |
| Mono.Cecil 0.11.6 | External build dependency, distributed by its authors under MIT. Obtain it from the package link in the build guide. |
| Odin Inspector / Sirenix and other player dependencies | Remain in the separate game installation under their existing licenses; not vendored. |
| Screenshots under `docs/images/` | Captures of the existing game used to demonstrate the refinement. They are excluded from the source-code MIT grant. |
| `assets/clay-ui.png` | Newly generated clay UI artwork for this project, shared under the repository's MIT terms. Generation method and prompt are recorded in `docs/ARTWORK.md`. |

For Mono.Cecil attribution and license text, consult the [upstream license](https://github.com/jbevain/cecil/blob/master/LICENSE.txt). Preserve upstream notices when redistributing dependencies yourself.
