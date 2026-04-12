FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY . ./

WORKDIR /app/model
ENV PYTHONUNBUFFERED=1

CMD ["python", "rabbitmq_reader_service.py"]
