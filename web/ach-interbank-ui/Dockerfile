FROM node:24.21.0-alpine AS build
ARG NPM_VERSION=12.1.0
WORKDIR /app
COPY package*.json ./
RUN apk add --no-cache curl \
    && npm install -g npm@"${NPM_VERSION}"
RUN npm ci --no-audit --no-fund --loglevel=error
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.30.5-alpine
RUN apk add --no-cache curl
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build /app/dist/ach-interbank-ui/ /usr/share/nginx/html
EXPOSE 80
