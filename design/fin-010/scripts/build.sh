#!/bin/bash
# Reconstruye berrutti.op desde cero (en el servidor ya iniciado): variables, páginas, tableros y pantallas, y corre refine.
# Requiere: op start --headless --file design/fin-010/berrutti.op. Páginas: n1 = Sistema, n2 = Mobile (ver op page list).
set -e
export PATH=$PATH:/c/Users/User/.openpencil/bin
d=$(cd "$(dirname "$0")" && pwd)
python -I "$d/tokens.py" >/dev/null
op vars:set @"$d/vars.json" --replace >/dev/null
"$d/wipe.sh" n1; "$d/wipe.sh" n2
for f in s1 s2; do "$d/run.sh" n1 $f.js >/dev/null; done
for f in m1 m2; do "$d/run.sh" n2 $f.js >/dev/null; done
for pg in n1 n2; do
  for id in $(op get --page $pg --depth 0 | python -I -c "import sys,json; print(' '.join(n['id'] for n in json.load(sys.stdin)['nodes']))"); do
    op design:refine --root-id $id --page $pg > /dev/null
    # refine fija la altura de los tableros en 4 px; se restablece fit_content
    [ $pg = n1 ] && op update $id '{"height":"fit_content"}' --page $pg >/dev/null
  done
done
op set_active_page index=0 >/dev/null
