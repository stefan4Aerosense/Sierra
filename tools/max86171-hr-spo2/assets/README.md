# Fonts shipped with the tester

`ChakraPetch-BoldItalic.ttf` — Chakra Petch, by Cadson Demak, from Google Fonts.
Licensed under the SIL Open Font License 1.1 (`OFL.txt`), which allows redistribution inside this tool.
Used for the large heart-rate and SpO2 numbers, drawn stretched 1.3x wide to match the look Stefan asked for.
It is embedded into the exe at build time (`/res:` in `build.cmd`) and loaded from memory, so the built
`HeartRateTester.exe` stays a single self-contained file with no font to install on the machine.
