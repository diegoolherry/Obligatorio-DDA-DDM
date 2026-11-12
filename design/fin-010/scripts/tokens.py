import json, sys, os
D=os.path.dirname(os.path.abspath(__file__))
def lum(h):
    h=h.lstrip('#'); c=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    c=[x/12.92 if x<=0.03928 else ((x+0.055)/1.055)**2.4 for x in c]
    return 0.2126*c[0]+0.7152*c[1]+0.0722*c[2]
def cr(a,b):
    la,lb=lum(a),lum(b); 
    if la<lb: la,lb=lb,la
    return (la+0.05)/(lb+0.05)
T={
"primary-50":"#FDECEC","primary-100":"#FAD2D3","primary-500":"#D93036","primary-600":"#C41E24","primary-700":"#A3171C","primary-900":"#5C0B0E",
"accent-100":"#FCE7D6","accent-500":"#E77723","accent-700":"#9A4A0C",
"ink-900":"#141414","ink-700":"#3D3D3A","ink-500":"#5E5E59","ink-300":"#8A8A84",
"surface-0":"#FFFFFF","surface-50":"#F6F6F3","surface-100":"#ECECE8","border-200":"#D6D6D0",
"success-100":"#E2F3E7","success-700":"#1B6B38",
"error-100":"#FCE8E6","error-700":"#B3261E","error-800":"#8A1C16",
"warning-100":"#FFF0C2","warning-500":"#F2B01E","warning-800":"#6E4400",
"info-100":"#E3EEFB","info-700":"#1D4F91",
}
pairs=[("ink-900","surface-0"),("ink-900","surface-50"),("ink-700","surface-0"),("ink-500","surface-0"),("ink-500","surface-50"),("ink-500","surface-100"),("ink-300","surface-0"),
("surface-0","primary-600"),("surface-0","primary-700"),("primary-600","surface-0"),("primary-700","surface-0"),("primary-700","primary-50"),("primary-700","primary-100"),
("ink-900","accent-500"),("accent-700","surface-0"),("accent-700","accent-100"),("ink-900","accent-100"),("surface-0","ink-900"),("surface-0","ink-700"),("accent-500","ink-900"),("accent-500","surface-0"),
("surface-0","primary-500"),("ink-900","primary-100"),
("success-700","success-100"),("success-700","surface-0"),("surface-0","success-700"),
("error-700","error-100"),("error-700","surface-0"),("surface-0","error-700"),
("warning-800","warning-100"),("warning-800","surface-0"),("ink-900","warning-500"),
("info-700","info-100"),("info-700","surface-0"),("surface-0","info-700"),
("border-200","surface-0"),("ink-700","surface-100"),("surface-0","error-800"),("ink-900","surface-100"),("primary-700","surface-0"),("primary-700","surface-50"),("ink-900","primary-50"),("ink-300","surface-0"),("accent-500","surface-50"),("primary-100","ink-900"),("accent-500","ink-900")]
out=[]
for f,b in pairs:
    r=cr(T[f],T[b]); out.append((f,b,round(r,2)))
    print(f"{f:12}{T[f]} on {b:12}{T[b]} {r:5.2f}", "AA" if r>=4.5 else ("AA-large/UI" if r>=3 else "FAIL"))
json.dump({"tokens":T,"pairs":out},open(D+"/tokens.json","w"),indent=1)

V={k:{"type":"color","value":v} for k,v in T.items()}
for k,v in {"1":4,"2":8,"3":12,"4":16,"5":20,"6":24,"8":32,"10":40,"12":48}.items(): V["spacing-"+k]={"type":"number","value":v}
for k,v in {"sm":8,"md":12,"lg":16,"xl":24,"full":999}.items(): V["radius-"+k]={"type":"number","value":v}
for k,v in {"caption":12,"body-sm":14,"body":16,"subtitle":18,"heading":20,"title":24,"display":32}.items(): V["text-"+k]={"type":"number","value":v}
V["font-sans"]={"type":"string","value":"Inter"}
V["shadow-sm"]={"type":"string","value":"0 1 3 0 rgba(20,20,20,0.08)"}
V["shadow-md"]={"type":"string","value":"0 4 12 0 rgba(20,20,20,0.12)"}
V["shadow-lg"]={"type":"string","value":"0 8 24 -4 rgba(20,20,20,0.18)"}
json.dump(V,open(D+"/vars.json","w"),indent=1)
open(D+"/tokens.js","w").write("const TOK = "+json.dumps(T)+";\n")
