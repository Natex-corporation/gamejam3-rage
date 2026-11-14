# Third-party notices and scope

The root MIT license covers the original refinement source, scripts, and documentation in this repository. It does not grant rights to external dependencies or relicense the original game.

| Material | Treatment |
| --- | --- |
| Unity player and managed libraries | Included in the standalone player release, never committed to the source repository. Governed by Unity's applicable terms. |
| Original game assembly, scenes, artwork, and audio | Not included in the source checkout. Included in the standalone player release under their existing terms; they are not covered by this repository’s MIT grant. |
| StrawberryMuffins font | Reused from assets already loaded by the game. The standalone player retains the original font assets; its original license applies. |
| Mono.Cecil 0.11.6 | External build dependency, distributed by its authors under MIT. The developer-kit release includes it and its upstream license. |
| Odin Inspector / Sirenix and other player dependencies | Remain in the standalone player under their existing licenses; not vendored. |
| Screenshots under `docs/images/` | Captures of the existing game used to demonstrate the refinement. They are excluded from the source-code MIT grant. |
| `assets/clay-ui.png` and `assets/kiln-king.png` | Newly generated clay UI and boss artwork for this project, shared under the repository's MIT terms. Generation method and prompt are recorded in `docs/ARTWORK.md`. |

For Mono.Cecil attribution and license text, consult the [upstream license](https://github.com/jbevain/cecil/blob/master/LICENSE.txt). Preserve upstream notices when redistributing dependencies yourself.
