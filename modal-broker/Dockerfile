# Build context: modal-broker
FROM node:22-bookworm-slim
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev
COPY src ./src
ENV PORT=8080 NODE_ENV=production
EXPOSE 8080
USER node
CMD ["node", "src/server.mjs"]
