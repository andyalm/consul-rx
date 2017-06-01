FROM alpine

RUN apk add --update bash curl && rm -rf /var/cache/apk/*

COPY register-service.sh /
COPY set-kv.sh /
COPY seed.sh /
CMD ["/seed.sh"]