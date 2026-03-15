import os
import re
import argparse
from dataclasses import dataclass, field
from pathlib import Path
from pprint import pprint
from re import RegexFlag

matcher = re.compile(r"\"(\S+)\":\s+\"([\S\s]+)\",?(?:\s*(//\s*todo))?", RegexFlag.IGNORECASE)


@dataclass
class TranslationEntry:
    key: str
    value: str
    comments: list[str]
    is_todo: bool = field(default=False)


@dataclass
class ParsedTranslationFile:
    entries: dict[str, TranslationEntry]
    key_order: list[str]

def full_strip(string: str) -> str:
    # For some reason there's a zero-width space (0xFEFF) at the start of some of the json files???
    return string.strip().replace(chr(0xFEFF), '').replace(chr(0x200B), '')


def parse_to_entries(lines: list[str]) -> ParsedTranslationFile:
    entries: dict[str, TranslationEntry] = {}
    key_order: list[str] = []
    cur_comments: list[str] = []
    for translation_line in lines:
        line = full_strip(translation_line)
        if not line or line.startswith("{") or line.startswith("}"):
            continue
        if line.startswith("//"):
            cur_comments.append(full_strip(line[2:]))
            continue
        matches = matcher.match(line)
        if not matches:
            print(f"Unknown line: {line}")
            continue
        key = matches.group(1)
        value = matches.group(2)
        todo_group = matches.group(3)
        is_todo = False
        if todo_group is not None and "todo" in matches.group(3).lower():
            is_todo = True
        if key in entries:
            raise ValueError(f"Duplicate key found: {key}, it might be overwritten")
        entries[key] = TranslationEntry(key, value, cur_comments, is_todo=is_todo)
        key_order.append(key)
        cur_comments = []
    return ParsedTranslationFile(entries, key_order)


def write_entries_to_file(parsed: ParsedTranslationFile, output_file: Path):
    """Write translation entries to a JSON file."""
    indent = " " * 4
    with output_file.open('w') as f:
        f.write("{\n")
        for idx, key in enumerate(parsed.key_order):
            is_last = idx == len(parsed.key_order) - 1
            entry = parsed.entries[key]
            for comment in entry.comments:
                f.write(f'{indent}// {comment}\n')
            f.write(f'{indent}"{key}": "{entry.value}"')
            if not is_last:
                f.write(",")
            if entry.is_todo:
                f.write(" // TODO")
            f.write("\n")
        f.write("}\n")


def main():
    parser = argparse.ArgumentParser(
        description='Sync translation files with default.json and track translation progress.'
    )
    parser.add_argument(
        'directory',
        type=str,
        nargs='?',
        default=os.getcwd(),
        help='Path to the directory containing translation files (default: current working directory)'
    )

    args = parser.parse_args()

    translations_dir = Path(args.directory).resolve()

    if not translations_dir.exists():
        raise FileNotFoundError(f"Directory not found: {translations_dir}")

    if not translations_dir.is_dir():
        raise NotADirectoryError(f"Path is not a directory: {translations_dir}")

    default_translation_file = translations_dir / "default.json"
    if not default_translation_file.exists():
        raise FileNotFoundError(f"Missing default.json in {translations_dir}")

    default_entries: ParsedTranslationFile
    with default_translation_file.open('r') as f:
        default_entries = parse_to_entries(f.readlines())

    translation_progress: dict[str, float] = {}
    other_files = list(translations_dir.iterdir())
    for other_translation in other_files:
        if other_translation.suffix != ".json" or other_translation.stem == "default":
            print(f'Skipping file {other_translation.name}')
            continue
        with other_translation.open('r') as f:
            translated_entries = parse_to_entries(f.readlines())
            translated_entries.key_order = default_entries.key_order

        already_translated = 0
        for key in default_entries.key_order:
            if key in translated_entries.entries and not translated_entries.entries[key].is_todo:
                # Skip keys that have already been translated
                already_translated += 1
                continue
            print(f'Key {key} not found in {other_translation.name}, adding from default.json')
            default_entry = default_entries.entries[key]
            translated_entries.entries[key] = TranslationEntry(default_entry.key, default_entry.value,
                                                               default_entry.comments, is_todo=True)

        write_entries_to_file(translated_entries, other_translation)

        pct_translated = already_translated / len(default_entries.key_order) * 100
        translation_progress[other_translation.stem.upper()] = pct_translated
        print(f'File {other_translation.name} translated {pct_translated:.2f}%')
    pprint(f'Translation progress: {translation_progress}')
    print(f'Total files processed: {len(other_files)}')


if __name__ == '__main__':
    main()
