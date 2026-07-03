import os
import json
import ssl
from enum import StrEnum
from urllib import error, parse, request
from uuid import uuid4

from fastapi import BackgroundTasks, FastAPI, HTTPException, status
from pydantic import BaseModel

from face_recognition_knn import predict

app = FastAPI()
root_path = "C:/WebGallery/Data"
minimal_api_url = "https://localhost:5041"


class JobStatus(StrEnum):
    IN_PROGRESS = "InProgress"
    COMPLETED = "Completed"
    FAILED = "Failed"


job_statuses: dict[str, JobStatus] = {}

class Album(BaseModel):
    user: str
    album: str


def post_face_tag(album: Album, media_locator: str, recognized_person_name: str) -> None:
    user_segment = parse.quote(album.user, safe="")
    album_segment = parse.quote(album.album, safe="")
    media_segment = parse.quote(media_locator, safe="")

    url = (
        f"{minimal_api_url}/users/{user_segment}/albums/{album_segment}/"
        f"{media_segment}/tags"
    )
    body = json.dumps({"tagName": recognized_person_name}).encode("utf-8")
    headers = {"Content-Type": "application/json"}
    req = request.Request(url=url, data=body, headers=headers, method="POST")

    # Localhost HTTPS often uses a dev certificate; this avoids TLS trust failures.
    ssl_context = ssl._create_unverified_context()
    try:
        with request.urlopen(req, timeout=10, context=ssl_context) as response:
            if response.status < 200 or response.status >= 300:
                raise RuntimeError(
                    f"Failed to add tag '{recognized_person_name}' for '{media_locator}'. "
                    f"Status code: {response.status}."
                )
    except error.HTTPError as ex:
        raise RuntimeError(
            f"Failed to add tag '{recognized_person_name}' for '{media_locator}'. "
            f"Status code: {ex.code}."
        ) from ex
    except error.URLError as ex:
        raise RuntimeError(
            f"Failed to reach Minimal API while tagging '{media_locator}': {ex.reason}"
        ) from ex


def process_album_faces(job_id: str, album_path: str, album: Album) -> None:
    try:
        for image_file in os.listdir(album_path):
            full_file_path = os.path.join(album_path, image_file)

            if not os.path.isfile(full_file_path):
                continue

            print(f"Processing image: {full_file_path}")
            predictions = predict(full_file_path, model_path="trained_knn_model.clf", distance_threshold=0.51)
            posted_tag_names: set[str] = set()

            for name, (top, right, bottom, left) in predictions:
                if name != "unknown" and name not in posted_tag_names:
                    print(f"- Match found: {name} in {image_file} at ({left}, {top})")
                    
                    post_face_tag(album, image_file, name)
                    posted_tag_names.add(name)

                # else:
                #     print(f"- Found {name} at ({left}, {top})")
    except Exception:
        job_statuses[job_id] = JobStatus.FAILED
        raise

    job_statuses[job_id] = JobStatus.COMPLETED

@app.get("/")
def read_root():
    return {"Message": "This is the Face Recognition API. Use the /docs endpoint to explore the available endpoints."}

@app.get("/jobs/face-recognition/{job_id}")
async def get_face_recognition_job(job_id: str):
    job_status = job_statuses.get(job_id)
    if job_status is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Job not found.")

    return {
        "jobId": job_id,
        "status": job_status,
    }


@app.post("/jobs/face-recognition", status_code=status.HTTP_202_ACCEPTED)
async def face_recognition(album: Album, background_tasks: BackgroundTasks):
    album_path = f"{root_path}/{album.user}/{album.album}"
    if not os.path.exists(album_path):
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Album path does not exist.")

    job_id = str(uuid4())
    job_statuses[job_id] = JobStatus.IN_PROGRESS
    background_tasks.add_task(process_album_faces, job_id, album_path, album)

    return {
        "jobId": job_id,
        "status": JobStatus.IN_PROGRESS,
        "album": album.model_dump(),
    }
