ARG base=ubuntu:latest
FROM ${base}

# workaround for coturn/coturn image (nobody default)
USER root

RUN apt-get update && apt-get install \
    tmux \
    mc \
    iproute2 \
    iputils-ping \
    net-tools \
    dnsutils \
    ncat \
    lsof \
    procps \
    psutils \
    wget \
    htop \
    git \
    -y
    #&& apt-get clean && rm -rf /var/lib/apt/lists/*

# https://go.dev/doc/install
# https://medium.com/@prathameshbagul/solving-the-package-crypto-ecdh-is-not-in-goroot-error-in-linux-quick-and-easy-fix-e6a0211c6354
RUN wget https://dl.google.com/go/go1.24.1.linux-amd64.tar.gz \
    && rm -rf /usr/local/go \
    && tar -C /usr/local -xzf go1.24.1.linux-amd64.tar.gz \
    && rm -f go1.24.1.linux-amd64.tar.gz \
    && true

# coturn - for diagnostics tools turnutils_uclient/turnutils_peer https://gist.github.com/cameronelliott/be1e581cb7b28f748e04bcabc249e6b6
#   ncat -lk --sh-exec 'read line && echo "${line}-$(date)" && echo ${line} > /dev/stderr' -u 0.0.0.0 8888
#   tcpdump -i any -vv -X -n udp # -k to show interfaces on macOS
#   tcpdump -i any -vv -X -n udp and ip net 172.21.0.0/16
RUN apt-get install \
    coturn \
    tcpdump \
    -y

# fzf
RUN git clone --depth 1 https://github.com/junegunn/fzf.git ~/.fzf \
    && ~/.fzf/install

ENV GOROOT="/usr/local/go"
ENV GOPATH="/root/go"
ENV PATH="$GOPATH/bin:$GOROOT/bin:$PATH"

# # https://github.com/pion/webrtc/tree/master/examples/pion-to-pion
# RUN go install github.com/pion/webrtc/v4/examples/pion-to-pion/answer@latest
# RUN go install github.com/pion/webrtc/v4/examples/pion-to-pion/offer@latest
# # https://github.com/pion/webrtc/tree/master/examples/ortc
# RUN go install github.com/pion/webrtc/v4/examples/ortc@latest

# extended example with diagnostics and stun/turn option
COPY rt /src/rt
RUN go install -C /src/rt

# Useful setup
RUN echo "set-option -g mouse on" >> /root/.tmux.conf

RUN echo "export GOROOT=/usr/local/go" >> /root/.profile
RUN echo "export GOPATH=\$HOME/go" >> /root/.profile
RUN echo "export PATH=\$GOPATH/bin:\$GOROOT/bin:\$PATH" >> /root/.profile

RUN echo "alias ta='tmux new -As0'" >> /root/.bashrc

# Expose WebRTC UDP ports
#EXPOSE 3478/udp
#EXPOSE 5349/udp
#EXPOSE 8888/tcp
#EXPOSE 49152-65535/udp
