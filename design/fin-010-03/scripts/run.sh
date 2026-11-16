#!/bin/bash
# uso: scripts/run.sh <pagina> <script.js>   (concatena prelude.js + web.js + script)
export PATH=$PATH:/c/Users/User/.openpencil/bin
d=$(dirname "$0"); out=$(mktemp --suffix=.js)
cat "$d/prelude.js" "$d/web.js" "$d/home-common.js" "$d/n-common.js" "$d/$2" > "$out"
op design @"$out" --page "$1" | python -I -c "import sys,json; d=json.load(sys.stdin); print(\"nodos\", d.get(\"nodeCount\"), d.get(\"error\", \"\"))"
rm -f "$out"
