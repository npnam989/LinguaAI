# 🚀 LinguaAI Deployment Guide - Railway/Render

## 📋 Prerequisites
- GitHub account
- Railway or Render account
- Gemini API key

---

## 🔒 Security Applied
✅ Rate limiting (30 req/min per IP)
✅ Security headers (XSS, CSRF, Clickjacking)
✅ Environment-based CORS
✅ HTTPS redirection in production
✅ API key via environment variables

---

## 🚂 Deploy to Railway

### Step 1: Push to GitHub
```bash
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/YOUR_USERNAME/linguaai.git
git push -u origin main
```

### Step 2: Deploy API
1. Go to [railway.app](https://railway.app)
2. New Project → Deploy from GitHub
3. Select your repo
4. Click on service → Settings:
   - **Root Directory**: `src/LinguaAI.Api`
   - **Build Command**: `dotnet publish -c Release -o out`
   - **Start Command**: `dotnet out/LinguaAI.Api.dll`
5. Add Environment Variables:
   ```
   Gemini__ApiKey = YOUR_GEMINI_API_KEY
   Cors__AllowedOrigins = https://your-web-app.railway.app
   ASPNETCORE_ENVIRONMENT = Production
   ```
6. Copy deployed URL (e.g., `https://linguaai-api.railway.app`)

### Step 3: Deploy Web
1. Add New Service → From same repo
2. Settings:
   - **Root Directory**: `src/LinguaAI.Web`
   - **Build Command**: `dotnet publish -c Release -o out`
   - **Start Command**: `dotnet out/LinguaAI.Web.dll`
3. Add Environment Variables:
   ```
   ApiBaseUrl = https://linguaai-api.railway.app
   ASPNETCORE_ENVIRONMENT = Production
   ```

---

## 🎨 Deploy to Render

### API Service
1. Go to [render.com](https://render.com)
2. New → Web Service → Connect GitHub
3. Settings:
   - **Root Directory**: `src/LinguaAI.Api`
   - **Runtime**: Docker
   - **Instance Type**: Free
4. Environment Variables:
   ```
   Gemini__ApiKey = YOUR_API_KEY
   Cors__AllowedOrigins = https://your-web.onrender.com
   ```

### Web Service
1. New → Web Service
2. Settings:
   - **Root Directory**: `src/LinguaAI.Web`
   - **Runtime**: Docker
4. Environment Variables:
   ```
   ApiBaseUrl = https://your-api.onrender.com
   ```

---

## ⚙️ Environment Variables Reference

### API
| Variable | Description | Example |
|----------|-------------|---------|
| `Gemini__ApiKey` | Gemini API key | `AIzaSy...` |
| `Cors__AllowedOrigins` | Allowed origins (comma-separated) | `https://web.railway.app` |
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |

### Web
| Variable | Description | Example |
|----------|-------------|---------|
| `ApiBaseUrl` | API URL | `https://api.railway.app` |
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |

---

## 🧪 Test Deployment
```bash
# Health check
curl https://your-api.railway.app/health

# Test vocabulary endpoint
curl -X POST https://your-api.railway.app/api/vocabulary/generate \
  -H "Content-Type: application/json" \
  -d '{"language":"ko","theme":"greetings","count":5}'
```
