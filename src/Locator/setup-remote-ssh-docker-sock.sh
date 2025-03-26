#!/usr/bin/env sh

# Forwarding the Docker Socket over SSH
#   https://medium.com/@dperny/forwarding-the-docker-socket-over-ssh-e6567cfab160
#
# This workaround allows debugging over docker in remote host
#   as Docker.DotNet package doesn't support ssh docker context natively
[[ -z "$1" ]] && echo "Usage: $0 <user@host>" && exit 1

set -x
ssh -nNT -L $(pwd)/docker.sock:/var/run/docker.sock $1
