#!/usr/bin/env python
import argparse
import os
import re
import sqlite3

DIRNAME_RE = re.compile("^(.*) [(](.*)[)]$")


def scrub(dir: str, verbose: bool = False) -> None:
    with sqlite3.connect(
        os.path.join(dir, "Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin.db")
    ) as conn:
        for dirpath, _, filenames in os.walk(dir):
            if dirpath == dir:
                continue
            dirname_match = DIRNAME_RE.match(dirpath)
            if dirname_match is None:
                print(f"Unknown directory {dirpath}")
                continue
            dirname_id = dirname_match.group(2)
            (series_id,) = conn.execute(
                "SELECT `Id` FROM `Series` WHERE `Metadata_SeriesId` = ?", (dirname_id,)
            ).fetchone()
            for video_filename in (f for f in filenames if not f.endswith(".json")):
                res = list(
                    conn.execute(
                        "SELECT `Id`, `DownloadInterrupted`, `DownloadReason`, `DeleteReason`"
                        " FROM `Episodes`"
                        " WHERE `SeriesId` = ? AND `Metadata_Filename` = ?",
                        (series_id, video_filename),
                    )
                )
                video_path = os.path.join(dirpath, video_filename)
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
