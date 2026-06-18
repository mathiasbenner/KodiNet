FROM python:3.12-alpine

# Minimal Dependencies
RUN apk add --no-cache gcc musl-dev libffi-dev

WORKDIR /app

# Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .

# Volume mounted
RUN mkdir -p /data/videos

EXPOSE 8090

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8090", "--timeout-keep-alive", "300"]