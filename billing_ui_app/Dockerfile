FROM node:23.11.0 as build

WORKDIR /app

COPY package*.json ./

# COPY kendo-ui-license.txt ./

RUN npm install

RUN npm install -g @angular/cli

ARG KENDO_UI_LICENSE

RUN npx kendo-ui-license activate

COPY . .

ARG ENVIRONMENT

# ✅ ADD: Fix Angular memory issue
ENV NODE_OPTIONS="--max_old_space_size=4096"

RUN ng build --configuration=$ENVIRONMENT

FROM nginx:latest

# ✅ FIX: correct build path (added /app)
COPY --from=build /app/dist/rethinkbilling /usr/share/nginx/html

#COPY --from=build app/dist/rethinkbilling /usr/share/nginx/html

# Copy the nginx file to fix fallback issue on refreshing.
COPY /nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80