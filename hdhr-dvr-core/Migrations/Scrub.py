#!/usr/bin/env python
import argparse
import os
import re
import sqlite3

FILENAME_RE = re.compile(
    "^.*"
    "[/\\\\]"
    "([^/\\\\]+) \\[([^/\\\\\\]]+)\\]"
    "[/\\\\]"
    "(?:Season ([0-9]+))?"
    "[/\\\\]?"
    "([^/\\\\]+) - \\[([^/\\\\\\]]+)\\]([.][^./\\\\]+)$"
)
SERIES_EPISODE_RE = re.compile("^(.+) S([0-9]+)E([0-9]+)")


def scrub(dir: str, verbose: bool = False) -> None:
    with sqlite3.connect(
        os.path.join(dir, "Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin.db")
    ) as conn:
        for dirpath, _, filenames in os.walk(dir):
            if dirpath == dir:
                continue
            for video_filename in filenames:
                video_path = os.path.join(dirpath, video_filename)
                match = FILENAME_RE.match(video_path)
                if match is None:
                    print(f"Unable to parse file path {video_path}")
                    continue
                series_title_1 = match.group(1)
                series_id = match.group(2)
                season_num_1 = match.group(3)
                series_title_2 = match.group(4)
                tag = match.group(5)
                file_ext = match.group(6)
                series_episode_match = SERIES_EPISODE_RE.match(series_title_2)
                if series_episode_match is not None:
                    series_title_2 = series_episode_match.group(1)
                    season_num_2 = series_episode_match.group(2)
                    episode_num = series_episode_match.group(3)
                else:
                    season_num_2 = None
                    episode_num = None
                res = list(
                    conn.execute(
                        "SELECT `Episodes`.`Id`, `Series`.`Metadata_Title`, `Episodes`.`Metadata_Filename`"
                        " FROM `Episodes`"
                        " INNER JOIN `Series` ON `Series`.`Id` = `Episodes`.`SeriesId`"
                        " WHERE `Series`.`Metadata_SeriesId` = ?"
                        f" AND `Episodes`.`Metadata_EpisodeNumber` {'ISNULL' if episode_num is None else '= ?'}",
                        tuple(
                            v
                            for v in (
                                series_id,
                                (
                                    f"S{season_num_2}E{episode_num}"
                                    if episode_num is not None
                                    else None
                                ),
                            )
                            if v is not None
                        ),
                    )
                )
                res_title = ""
                if len(res) > 0:
                    res_title: str = res[0][1]
                    for char in '<>:"/\\|?*':
                        res_title = res_title.replace(char, "")
                if series_title_1 != series_title_2 or (
                    len(res) > 0 and series_title_1 != res_title
                ):
                    print(f"Title mismatch for {video_path}")
                if season_num_1 != season_num_2 or (season_num_2 is None) != (
                    episode_num is None
                ):
                    print(f"Season number mismatch for {video_path}")
                if len(res) > 0:
                    for row in res:
                        if tag in row[2] and row[2].endswith(file_ext):
                            break
                    else:
                        print(f"Filename mismatch for {video_path}")
                match len(res):
                    case 0:
                        print(f"Missing database entry for {video_path}")
                    case 1 if verbose:
                        print(f"Exactly one entry for {video_path}")
                    case _ if verbose:
                        print(f"Multiple entries for {video_path}")
                        print(
                            "    "
                            + " => ".join(":".join(str(v) for v in r) for r in res)
                        )
                    case _:
                        pass


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        "Scrub a database to make sure it's not missing entries"
    )
    parser.add_argument("dir", help="root directory of library")
    parser.add_argument(
        "-v",
        action="store_true",
        help="print information about valid entries",
        dest="verbose",
    )
    parsed = parser.parse_args()
    scrub(parsed.dir, parsed.verbose)
