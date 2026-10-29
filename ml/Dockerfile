# Detection service image (CPU). Build: docker build -t aerialwatch-ml .  Run: docker run -p 8000:8000 -v %cd%/models:/models -e AW_MODEL_PATH=/models/<file>.pt aerialwatch-ml
FROM python:3.11-slim
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libgl1 libglib2.0-0 && rm -rf /var/lib/apt/lists/*
COPY requirements.txt requirements-train.txt ./
RUN pip install --no-cache-dir -r requirements-train.txt
COPY aerialwatch_ml ./aerialwatch_ml
EXPOSE 8000
CMD ["uvicorn", "aerialwatch_ml.service:app", "--host", "0.0.0.0", "--port", "8000"]
