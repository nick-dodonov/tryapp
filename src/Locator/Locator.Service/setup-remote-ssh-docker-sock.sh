#!/usr/bin/env bash

# Forwarding the Docker Socket over SSH
#   https://medium.com/@dperny/forwarding-the-docker-socket-over-ssh-e6567cfab160
#
# This workaround allows debugging over docker in remote host
#   as Docker.DotNet package doesn't support ssh docker context natively
[[ -z "$1" ]] && echo "Usage: $0 <user@host>" && exit 1

set -x

# -n  Redirects stdin from /dev/null
# -N  Do not execute a remote command.  This is useful for just forwarding ports.
# -T  Disable pseudo-terminal allocation.
# -L  Specifies that connections to the given TCP port or Unix socket on the local (client) host are to be forwarded to the given host and port, or Unix socket, on the remote side.
ssh -nNT -L "$(pwd)/docker.sock:/var/run/docker.sock" "$1"
