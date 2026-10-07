#!/usr/bin/env python3
"""Informa un conjunto pequeño de advertencias de convenciones de clase."""

import argparse
import re
from pathlib import Path

RULES = {
    ".ts": [(r"\bany\b", "evitá TypeScript any")],
    ".tsx": [
        (r"\bany\b", "evitá TypeScript any"),
        (r"key\s*=\s*\{\s*(?:index|i)\s*\}", "usá una clave estable de dominio"),
        (r"</?(?:div|span|button|input|p)\b", "usá componentes de React Native, no HTML"),
    ],
    ".razor": [
        (r"new\s+\w+Service\s*\(", "inyectá servicios en lugar de construirlos"),
        (r'@on(?:click|change|input)\s*=\s*"[A-Za-z_]\w*\(\)"', "pasá el método de evento sin paréntesis"),
    ],
}


def scan(root: Path) -> list[str]:
    warnings: list[str] = []
    for path in root.rglob("*"):
        if not path.is_file() or path.suffix not in RULES or {".git", "node_modules", "bin", "obj"} & set(path.parts):
            continue
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            for pattern, message in RULES[path.suffix]:
                if re.search(pattern, line):
                    warnings.append(f"{path}:{number}: {message}")
    return warnings


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("root", nargs="?", default=".")
    parser.add_argument("--strict", action="store_true")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        cases = [(r"\bany\b", "let value: any"), (RULES[".razor"][1][0], '@onclick="Save()"')]
        assert all(re.search(pattern, sample) for pattern, sample in cases)
        print("Autoprueba superada")
        return 0
    warnings = scan(Path(args.root))
    print("\n".join(warnings) if warnings else "No se encontraron advertencias de convenciones")
    return 1 if args.strict and warnings else 0


if __name__ == "__main__":
    raise SystemExit(main())
