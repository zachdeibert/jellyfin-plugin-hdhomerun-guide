#!/usr/bin/env python
import argparse
import os
import re

SERIES_DIRNAME_RE = re.compile("^(.+) [(]([^)]+)[)]$")
EPISODE_FILENAME_RE = re.compile(
    "^.+ (S([0-9]+)E[0-9]+) (?:[0-9]+ )?(\\[[^\\]]+\\])([.][^.]+)$"
)
MOVIE_FILENAME_RE = re.compile("^.+ [0-9]+ (\\[[^\\]]+\\])([.][^.]+)$")


def rename_folders(dir: str, dry_run: bool = False, verbose: bool = False) -> None:
    for series_dirname in os.listdir(dir):
        series_dir = os.path.join(dir, series_dirname)
        series_match = SERIES_DIRNAME_RE.match(series_dirname)
        if series_match is None or not os.path.isdir(series_dir):
            continue
        series_name = series_match.group(1)
        series_id = series_match.group(2)
        target_series_dir = os.path.join(dir, f"{series_name} [{series_id}]")
        for video_filename in os.listdir(series_dir):
            video_file = os.path.join(series_dir, video_filename)
            episode_match = EPISODE_FILENAME_RE.match(video_filename)
            if episode_match is not None:
                episode_num_encoded = episode_match.group(1)
                series_num = int(episode_match.group(2))
                tag = episode_match.group(3)
                file_ext = episode_match.group(4)
            else:
                movie_match = MOVIE_FILENAME_RE.match(video_filename)
                if movie_match is None:
                    continue
                episode_num_encoded = None
                series_num = None
                tag = movie_match.group(1)
                file_ext = movie_match.group(2)
            if file_ext not in (".mpg",):
                continue
            target_file = os.path.join(
                *(
                    x
                    for x in (
                        target_series_dir,
                        f"Season {series_num:02d}" if series_num is not None else None,
                        f"{series_name}{' ' + episode_num_encoded if episode_num_encoded is not None else ''} - {tag}{file_ext}",
                    )
                    if x is not None
                )
            )
            if verbose:
                print(f"mv {video_file} {target_file}")
            if not dry_run:
                os.makedirs(os.path.dirname(target_file), exist_ok=True)
                os.rename(video_file, target_file)
        if len(os.listdir(series_dir)) == 0:
            if verbose:
                print(f"rmdir {series_dir}")
            if not dry_run:
                os.rmdir(series_dir)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Move video files into the correct folders"
    )
    parser.add_argument("dir", help="root directory of library")
    parser.add_argument(
        "-n", action="store_true", help="only print what would be done", dest="dry_run"
    )
    parser.add_argument(
        "-v",
        action="store_true",
        help="print each filename while processing",
        dest="verbose",
    )
    parsed = parser.parse_args()
    rename_folders(parsed.dir, parsed.dry_run, parsed.dry_run or parsed.verbose)
