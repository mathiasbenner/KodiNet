import os, shutil, stat
from pathlib import Path
from typing import Annotated

from fastapi import FastAPI, HTTPException, Header, UploadFile, File, Request
from fastapi.responses import FileResponse, StreamingResponse
from pydantic import BaseModel
from pydantic_settings import BaseSettings


# ── Configuration ─────────────────────────────────────────────────────────────

class Settings(BaseSettings):
    api_key:      str   = "api_key"
    root_path:    str   = "/data"
    max_size_mb:  int   = 2048   # limit upload Mo (2 Go by default)

    class Config:
        env_prefix = "STORAGE_"

settings = Settings()
ROOT     = Path(settings.root_path).resolve()
MAX_SIZE = settings.max_size_mb * 1024 * 1024

app = FastAPI(title="KodiNet Storage Server")


# ── Auth ──────────────────────────────────────────────────────────────────────

def check_auth(x_api_key: Annotated[str | None, Header()] = None):
    if settings.api_key and x_api_key != settings.api_key:
        raise HTTPException(status_code=401, detail="Clé API invalide")


# ── Helpers ───────────────────────────────────────────────────────────────────

def resolve(path: str) -> Path:
    """Résout le chemin et vérifie qu'il reste sous ROOT (path traversal)."""
    resolved = (ROOT / path.lstrip("/")).resolve()
    if not str(resolved).startswith(str(ROOT)):
        raise HTTPException(status_code=400, detail="Chemin interdit")
    return resolved


def entry_json(p: Path) -> dict:
    st = p.stat()
    return {
        "path":         "/" + str(p.relative_to(ROOT)).replace("\\", "/"),
        "name":         p.name,
        "isFolder":     p.is_dir(),
        "sizeBytes":    st.st_size if p.is_file() else 0,
        "lastModified": __import__("datetime").datetime.fromtimestamp(
                            st.st_mtime, tz=__import__("datetime").timezone.utc
                        ).isoformat(),
    }


# ── Endpoints ─────────────────────────────────────────────────────────────────

@app.get("/api/files")
def list_folder(path: str = "/", x_api_key: str | None = Header(None)):
    check_auth(x_api_key)
    folder = resolve(path)
    if not folder.exists() or not folder.is_dir():
        raise HTTPException(status_code=404, detail="Dossier introuvable")
    entries = sorted(folder.iterdir(), key=lambda p: (not p.is_dir(), p.name.lower()))
    return [entry_json(e) for e in entries]


@app.get("/api/files/download")
def download(path: str, x_api_key: str | None = Header(None)):
    check_auth(x_api_key)
    file = resolve(path)
    if not file.exists() or not file.is_file():
        raise HTTPException(status_code=404, detail="Fichier introuvable")
    return FileResponse(file, filename=file.name, media_type="application/octet-stream")


@app.post("/api/files/upload")
async def upload(
    path: str,
    file: UploadFile = File(...),
    x_api_key: str | None = Header(None),
):
    check_auth(x_api_key)
    folder = resolve(path)
    folder.mkdir(parents=True, exist_ok=True)
    dest = folder / file.filename

    # Vérifier la taille avant d'écrire (lecture par chunks)
    total = 0
    with dest.open("wb") as f:
        while chunk := await file.read(1024 * 1024):  # 1 Mo par chunk
            total += len(chunk)
            if total > MAX_SIZE:
                dest.unlink(missing_ok=True)
                raise HTTPException(
                    status_code=413,
                    detail=f"Fichier trop volumineux (max {settings.max_size_mb} Mo)"
                )
            f.write(chunk)

    return entry_json(dest)


@app.post("/api/files/folder")
def create_folder(path: str, x_api_key: str | None = Header(None)):
    check_auth(x_api_key)
    folder = resolve(path)
    folder.mkdir(parents=True, exist_ok=True)
    return entry_json(folder)


@app.delete("/api/files")
def delete(path: str, x_api_key: str | None = Header(None)):
    check_auth(x_api_key)
    target = resolve(path)
    if not target.exists():
        raise HTTPException(status_code=404, detail="Élément introuvable")
    if target.is_dir():
        shutil.rmtree(target)
    else:
        target.unlink()
    return {"deleted": True}


class RenameBody(BaseModel):
    path:    str
    newName: str

@app.patch("/api/files/rename")
def rename(body: RenameBody, x_api_key: str | None = Header(None)):
    check_auth(x_api_key)
    src  = resolve(body.path)
    dest = src.parent / body.newName
    if not src.exists():
        raise HTTPException(status_code=404, detail="Élément introuvable")
    if dest.exists():
        raise HTTPException(status_code=409, detail="Un élément avec ce nom existe déjà")
    src.rename(dest)
    return entry_json(dest)


class MoveBody(BaseModel):
    path:       str
    destFolder: str

@app.patch("/api/files/move")
def move(body: MoveBody, x_api_key: str | None = Header(None)):
    check_auth(x_api_key)
    src    = resolve(body.path)
    folder = resolve(body.destFolder)
    if not src.exists():
        raise HTTPException(status_code=404, detail="Élément introuvable")
    folder.mkdir(parents=True, exist_ok=True)
    dest = folder / src.name
    shutil.move(str(src), str(dest))
    return entry_json(dest)