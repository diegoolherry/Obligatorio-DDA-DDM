#!/bin/bash
# uso: scripts/wipe.sh <pagina>  — quita todos los nodos raíz de la página (op delete por nodo)
export PATH=$PATH:/c/Users/User/.openpencil/bin
for id in $(op get --page "$1" --depth 0 | python -I -c "import sys,json; print(' '.join(n['id'] for n in json.load(sys.stdin)['nodes']))"); do op delete "$id" --page "$1" >/dev/null; done
