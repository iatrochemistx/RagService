## Quickstart: Run the RAG API from Docker Hub

**Docker Hub:**  
[https://hub.docker.com/r/quantdevxx/ragservice](https://hub.docker.com/r/quantdevxx/ragservice)

---

### 1. Pull the Docker Image

```bash
docker pull quantdevxx/ragservice:latest
```

---

### 2. Run the Container

Replace `sk-...` with your OpenAI API key:

```bash
docker run -d --name ragservice-real   -e OPENAI__ApiKey=sk-...   -e ASPNETCORE_ENVIRONMENT=Development   -p 5241:8080 quantdevxx/ragservice:latest
```

---

### 3. Access the API

- Swagger UI: [http://localhost:5241/swagger](http://localhost:5241/swagger)
- REST Endpoint: `GET /query`
  - Parameters:
    - `q` (string): The user's query
    - `response` (boolean): Whether to include an LLM-generated answer

---

### How to Test

#### Option A: Swagger UI (Recommended)
Open your browser to [http://localhost:5241/swagger](http://localhost:5241/swagger)

- Click on `/query`
- Try it out with:
  - `q = Who discovered penicillin`
  - `response = true`

#### Option B: Curl (Terminal)
```bash
curl "http://localhost:5241/query?q=\"Who discovered penicillin\"&response=true"

```

---

### About the Data Folder

- This service loads documents from the `/data/` folder during container startup.
- Supported formats: `.txt`, `.md`, etc.
- Example files included:
  - `medicine.md` – contains facts about penicillin
  - `science.txt` – boiling point of water
- These files are embedded at build time. To add more:
  1. Drop your file in `src/RagService.Api/data/`
  2. Rebuild the Docker image using the steps below

---

### Rebuild Locally With Custom Data (Optional)

```bash
# From the root of the project
# (after adding files to src/RagService.Api/data/)
docker build -t quantdevxx/ragservice:latest .
```

Then re-run as described above.

---

### Environment Variables

- `OPENAI__ApiKey` – your OpenAI API key (**required**)
- `UseMocks` – set to `false` to use the real OpenAI API
- `ASPNETCORE_ENVIRONMENT=Development` enables Swagger UI

---

### Notes

- No source code or installation is needed to use the Docker image
- LLM answers come from OpenAI's Chat API (via API key)
- Retrieval uses OpenAI Embeddings + cosine similarity
- If `response=false`, you’ll get just the relevant documents
- If `response=true`, you’ll also get a generated answer

---
