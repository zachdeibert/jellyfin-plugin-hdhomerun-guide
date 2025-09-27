#!/usr/bin/env python
import argparse
import datetime
import enum
import itertools
import json
import os
import sqlite3
import time
import traceback
import typing
import urllib.error
import urllib.request


class DeleteReason(enum.IntEnum):
    NotDeleted = 0
    ReDownloaded = 1
    Downloaded = 2
    OneDayPassed = 3
    OneWeekPassed = 4
    Deleted = 5


class DownloadReason(enum.IntEnum):
    New = 0
    DownloadInterrupted = 1


class RecordingCategory(enum.IntEnum):
    Movie = 0
    Series = 1


class Series(typing.NamedTuple):
    Metadata_SeriesId: str
    Metadata_Title: str
    Metadata_Category: RecordingCategory
    Metadata_ImageUrl: str
    Metadata_PosterUrl: str | None
    Metadata_StartTime: datetime.datetime
    Metadata_IsNew: bool
    Metadata_Url: str


class Episode(typing.NamedTuple):
    Series: Series
    SeriesStartTime: datetime.datetime
    Metadata_Category: RecordingCategory
    Metadata_ChannelImageUrl: str | None
    Metadata_ChannelName: str
    Metadata_ChannelNumber: str
    Metadata_EndTime: datetime.datetime
    Metadata_EpisodeNumber: str | None
    Metadata_EpisodeTitle: str
    Metadata_FirstAiring: bool
    Metadata_ImageUrl: str
    Metadata_MovieScore: str | None
    Metadata_OriginalAirdate: datetime.datetime
    Metadata_PosterUrl: str | None
    Metadata_ProgramId: str
    Metadata_RecordEndTime: datetime.datetime
    Metadata_RecordError: str | None
    Metadata_RecordStartTime: datetime.datetime
    Metadata_RecordSuccess: bool
    Metadata_SeriesId: str
    Metadata_StartTime: datetime.datetime
    Metadata_Synopsis: str
    Metadata_Title: str
    Metadata_Filename: str
    Metadata_PlayUrl: str
    Metadata_CmdUrl: str
    DownloadInterrupted: bool
    DownloadStarted: datetime.datetime
    DownloadReason: DownloadReason
    DeleteReason: DeleteReason
    ReRecordable: bool


def get(json: dict[str, typing.Any], *key: str | typing.Any) -> typing.Any:
    for k in key:
        match k:
            case str():
                if k in json:
                    return json[k]
            case _:
                return k
    raise KeyError(key)


def gettime(value: int | str) -> datetime.datetime:
    match value:
        case int():
            return datetime.datetime.fromtimestamp(value)
        case str():
            return datetime.datetime.fromisoformat(value)


def parse(video_path: str, storage_path: str, episode_path: str) -> Episode:
    with open(storage_path, "r") as f:
        storage = json.load(f)
    with open(episode_path, "r") as f:
        episode = json.load(f)
    play_url = get(episode, "PlayURL", "PlayUrl")
    delete_reason = None
    while delete_reason is None:
        try:
            with urllib.request.urlopen(play_url) as f:
                delete_reason = DeleteReason.NotDeleted
        except urllib.error.HTTPError as e:
            match e.code:
                case 404:
                    delete_reason = DeleteReason.Downloaded
                case 503:
                    traceback.print_exc()
                    time.sleep(1)
                case _:
                    raise
    return Episode(
        Series=Series(
            Metadata_SeriesId=get(storage, "SeriesID", "SeriesId"),
            Metadata_Title=storage["Title"],
            Metadata_Category={
                "movie": RecordingCategory.Movie,
                "series": RecordingCategory.Series,
                RecordingCategory.Movie.value: RecordingCategory.Movie,
                RecordingCategory.Series.value: RecordingCategory.Series,
            }[storage["Category"]],
            Metadata_ImageUrl=get(storage, "ImageURL", "ImageUrl"),
            Metadata_PosterUrl=get(storage, "PosterURL", "PosterUrl", None),
            Metadata_StartTime=gettime(storage["StartTime"]),
            Metadata_IsNew=bool(get(storage, "New", "IsNew")),
            Metadata_Url=get(storage, "EpisodesURL", "Url"),
        ),
        SeriesStartTime=gettime(storage["StartTime"]),
        Metadata_Category={
            "movie": RecordingCategory.Movie,
            "series": RecordingCategory.Series,
            RecordingCategory.Movie.value: RecordingCategory.Movie,
            RecordingCategory.Series.value: RecordingCategory.Series,
        }[episode["Category"]],
        Metadata_ChannelImageUrl=get(
            episode, "ChannelImageURL", "ChannelImageUrl", None
        ),
        Metadata_ChannelName=episode["ChannelName"],
        Metadata_ChannelNumber=episode["ChannelNumber"],
        Metadata_EndTime=gettime(episode["EndTime"]),
        Metadata_EpisodeNumber=get(episode, "EpisodeNumber", None),
        Metadata_EpisodeTitle=get(episode, "EpisodeTitle", None),
        Metadata_FirstAiring=bool(get(episode, "FirstAiring", False)),
        Metadata_ImageUrl=get(episode, "ImageURL", "ImageUrl"),
        Metadata_MovieScore=get(episode, "MovieScore", None),
        Metadata_OriginalAirdate=gettime(get(episode, "OriginalAirdate", "StartTime")),
        Metadata_PosterUrl=get(episode, "PosterURL", "PosterUrl", None),
        Metadata_ProgramId=get(episode, "ProgramID", "ProgramId"),
        Metadata_RecordEndTime=gettime(episode["RecordEndTime"]),
        Metadata_RecordError=get(episode, "RecordError", None),
        Metadata_RecordStartTime=gettime(episode["RecordStartTime"]),
        Metadata_RecordSuccess=bool(get(episode, "RecordSuccess", True)),
        Metadata_SeriesId=get(episode, "SeriesID", "SeriesId"),
        Metadata_StartTime=gettime(episode["StartTime"]),
        Metadata_Synopsis=episode["Synopsis"],
        Metadata_Title=episode["Title"],
        Metadata_Filename=episode["Filename"],
        Metadata_PlayUrl=play_url,
        Metadata_CmdUrl=get(episode, "CmdURL", "CmdUrl"),
        DownloadInterrupted=False,
        DownloadStarted=datetime.datetime.fromtimestamp(
            os.stat(video_path).st_birthtime
        ),
        DownloadReason=DownloadReason.New,
        DeleteReason=delete_reason,
        ReRecordable=False,
    )


def import_json(dir: str, dry_run: bool = False, verbose: bool = False) -> None:
    with sqlite3.connect(
        os.path.join(dir, "Com.ZachDeibert.MediaTools.Hdhr.Dvr.Jellyfin.db")
    ) as conn:
        res = conn.execute("SELECT `Id` FROM `Series` ORDER BY `Id` DESC LIMIT 1")
        row = res.fetchone()
        (max_series_id,) = row if row is not None else (0,)
        for dirpath, _, filenames in list(os.walk(dir)):
            recycled_dir = os.path.join(
                dir, ".recycle-bin", os.path.relpath(dirpath, dir)
            )
            if not dry_run:
                os.makedirs(recycled_dir, exist_ok=True)
            for video_filename in (f for f in filenames if not f.endswith(".json")):
                episode_filename = f"{video_filename}.episode.json"
                storage_filename = f"{video_filename}.storage.json"
                if episode_filename in filenames and storage_filename in filenames:
                    video_path = os.path.join(dirpath, video_filename)
                    episode_path = os.path.join(dirpath, episode_filename)
                    storage_path = os.path.join(dirpath, storage_filename)
                    if verbose:
                        print(video_path)
                    episode = parse(video_path, storage_path, episode_path)
                    res = conn.execute(
                        "SELECT `Id` FROM `Series`"
                        " WHERE `Metadata_SeriesId` = ?"
                        " AND `Metadata_Title` = ?"
                        " AND `Metadata_Category` = ?"
                        " AND `Metadata_ImageUrl` = ?"
                        f" AND `Metadata_PosterUrl` {'ISNULL' if episode.Series.Metadata_PosterUrl is None else '= ?'}"
                        " AND `Metadata_IsNew` = ?"
                        " AND `Metadata_Url` = ?",
                        tuple(
                            v
                            for v in (
                                episode.Series.Metadata_SeriesId,
                                episode.Series.Metadata_Title,
                                episode.Series.Metadata_Category.value,
                                episode.Series.Metadata_ImageUrl,
                                episode.Series.Metadata_PosterUrl,
                                int(episode.Series.Metadata_IsNew),
                                episode.Series.Metadata_Url,
                            )
                            if v is not None
                        ),
                    )
                    row = res.fetchone()
                    if row is not None:
                        (series_id,) = row
                    else:
                        max_series_id += 1
                        series_id = max_series_id
                        sql = (
                            "INSERT INTO `Series` ("
                            "`Id`, "
                            "`Metadata_SeriesId`, "
                            "`Metadata_Title`, "
                            "`Metadata_Category`, "
                            "`Metadata_ImageUrl`, "
                            "`Metadata_PosterUrl`, "
                            "`Metadata_StartTime`, "
                            "`Metadata_IsNew`, "
                            "`Metadata_Url`"
                            ") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)"
                        )
                        values = (
                            series_id,
                            episode.Series.Metadata_SeriesId,
                            episode.Series.Metadata_Title,
                            episode.Series.Metadata_Category.value,
                            episode.Series.Metadata_ImageUrl,
                            episode.Series.Metadata_PosterUrl,
                            f"{episode.Series.Metadata_StartTime.isoformat(' ', 'seconds')}+00:00",
                            int(episode.Series.Metadata_IsNew),
                            episode.Series.Metadata_Url,
                        )
                        if dry_run:
                            print(
                                "".join(
                                    s
                                    for s in itertools.chain(
                                        *itertools.zip_longest(
                                            sql.split("?"), (repr(v) for v in values)
                                        )
                                    )
                                    if s is not None
                                )
                            )
                        else:
                            conn.execute(sql, values)
                    sql = (
                        "INSERT INTO `Episodes` ("
                        "`SeriesId`, "
                        "`SeriesStartTime`, "
                        "`Metadata_Category`, "
                        "`Metadata_ChannelImageUrl`, "
                        "`Metadata_ChannelName`, "
                        "`Metadata_ChannelNumber`, "
                        "`Metadata_EndTime`, "
                        "`Metadata_EpisodeNumber`, "
                        "`Metadata_EpisodeTitle`, "
                        "`Metadata_FirstAiring`, "
                        "`Metadata_ImageUrl`, "
                        "`Metadata_MovieScore`, "
                        "`Metadata_OriginalAirdate`, "
                        "`Metadata_PosterUrl`, "
                        "`Metadata_ProgramId`, "
                        "`Metadata_RecordEndTime`, "
                        "`Metadata_RecordError`, "
                        "`Metadata_RecordStartTime`, "
                        "`Metadata_RecordSuccess`, "
                        "`Metadata_SeriesId`, "
                        "`Metadata_StartTime`, "
                        "`Metadata_Synopsis`, "
                        "`Metadata_Title`, "
                        "`Metadata_Filename`, "
                        "`Metadata_PlayUrl`, "
                        "`Metadata_CmdUrl`, "
                        "`DownloadInterrupted`, "
                        "`DownloadStarted`, "
                        "`DownloadReason`, "
                        "`DeleteReason`, "
                        "`ReRecordable`"
                        ") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"
                    )
                    values = (
                        series_id,
                        f"{episode.Series.Metadata_StartTime.isoformat(' ', 'seconds')}+00:00",
                        episode.Metadata_Category.value,
                        episode.Metadata_ChannelImageUrl,
                        episode.Metadata_ChannelName,
                        episode.Metadata_ChannelNumber,
                        f"{episode.Metadata_EndTime.isoformat(' ', 'seconds')}+00:00",
                        episode.Metadata_EpisodeNumber,
                        episode.Metadata_EpisodeTitle,
                        int(episode.Metadata_FirstAiring),
                        episode.Metadata_ImageUrl,
                        episode.Metadata_MovieScore,
                        f"{episode.Metadata_OriginalAirdate.isoformat(' ', 'seconds')}+00:00",
                        episode.Metadata_PosterUrl,
                        episode.Metadata_ProgramId,
                        f"{episode.Metadata_RecordEndTime.isoformat(' ', 'seconds')}+00:00",
                        episode.Metadata_RecordError,
                        f"{episode.Metadata_RecordStartTime.isoformat(' ', 'seconds')}+00:00",
                        int(episode.Metadata_RecordSuccess),
                        episode.Metadata_SeriesId,
                        f"{episode.Metadata_StartTime.isoformat(' ', 'seconds')}+00:00",
                        episode.Metadata_Synopsis,
                        episode.Metadata_Title,
                        episode.Metadata_Filename,
                        episode.Metadata_PlayUrl,
                        episode.Metadata_CmdUrl,
                        int(episode.DownloadInterrupted),
                        f"{episode.DownloadStarted.isoformat(' ', 'seconds')}+00:00",
                        episode.DownloadReason.value,
                        episode.DeleteReason.value,
                        int(episode.ReRecordable),
                    )
                    recycled_episode = os.path.join(recycled_dir, episode_filename)
                    recycled_storage = os.path.join(recycled_dir, storage_filename)
                    if dry_run:
                        print(
                            "".join(
                                s
                                for s in itertools.chain(
                                    *itertools.zip_longest(
                                        sql.split("?"), (repr(v) for v in values)
                                    )
                                )
                                if s is not None
                            )
                        )
                        print(f"mv {episode_path} {recycled_episode}")
                        print(f"mv {storage_path} {recycled_storage}")
                    else:
                        conn.execute(sql, values)
                        os.rename(episode_path, recycled_episode)
                        os.rename(storage_path, recycled_storage)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Import JSON files to the SQLite database"
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
    import_json(parsed.dir, parsed.dry_run, parsed.verbose)
