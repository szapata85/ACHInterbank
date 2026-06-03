FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci --no-audit --no-fund --loglevel=error
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.30.1-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build /app/dist/ach-interbank-ui/ /usr/share/nginx/html
EXPOSE 80
