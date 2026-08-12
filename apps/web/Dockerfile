# Build the static bundle, then serve it from nginx, which also proxies the API so the whole
# app lives on one origin.
FROM node:20-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
# --ignore-scripts because postinstall runs Chakra's typegen, which needs src/theme.ts — and
# the source is copied below, after this layer, so it can be cached across source changes.
RUN npm ci --ignore-scripts

COPY . .

# Generates the Chakra type augmentations for the custom theme — theme.ts adds an `xs`
# breakpoint that the stock types do not know about, so `tsc` fails without this on a clean
# install. It writes into node_modules, so it has to run after npm ci and before the build.
RUN npm run typegen

# Vite bakes environment variables into the bundle at build time, so this is the one moment the
# API URL can be set. Empty on purpose: every call then becomes relative ("/api/...", "/Events")
# and nginx below routes it. That keeps one image working against any backend, and means there
# is no CORS to configure.
ENV WEREWOLF_SERVER_URL=""
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
