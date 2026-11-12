#!/bin/bash
# uso: scripts/fin.sh <pagina> — refine de cada raíz y restablece height fit_content (refine lo fija en un valor fijo)
export PATH=$PATH:/c/Users/User/.openpencil/bin
for id in $(op get --page "$1" --depth 0 | python -I -c "import sys,json; print(' '.join(n['id'] for n in json.load(sys.stdin)['nodes']))"); do
  echo "refine $id: $(op design:refine --root-id $id --page $1 | python -I -c "import sys,json; d=json.load(sys.stdin); print(d.get('totalNodeCount'), [f['fix'] for f in d.get('fixes',[])])")"
  op update $id '{"height":"fit_content"}' --page $1 >/dev/null
done
