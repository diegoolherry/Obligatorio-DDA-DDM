#!/bin/bash
# uso: scripts/run.sh <pagina> <script.js>   (concatena prelude.js + script)
export PATH=$PATH:/c/Users/User/.openpencil/bin
d=$(dirname "$0"); out=$(mktemp --suffix=.js)
cat "$d/tokens.js" "$d/prelude.js" "$d/$2" > "$out"
op design @"$out" --page "$1" | head -c 400
rm -f "$out"
