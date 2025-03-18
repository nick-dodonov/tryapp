FROM nginx:latest

RUN apt-get update && apt-get install -y \
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
    && true
    #&& apt-get clean && rm -rf /var/lib/apt/lists/*


# https://go.dev/doc/install
# https://medium.com/@prathameshbagul/solving-the-package-crypto-ecdh-is-not-in-goroot-error-in-linux-quick-and-easy-fix-e6a0211c6354
RUN wget https://dl.google.com/go/go1.24.1.linux-amd64.tar.gz \
    && rm -rf /usr/local/go \
    && tar -C /usr/local -xzf go1.24.1.linux-amd64.tar.gz \
    && rm -f go1.24.1.linux-amd64.tar.gz \
    && true

ENV GOROOT="/usr/local/go"
ENV GOPATH="/root/go"
ENV PATH="$GOPATH/bin:$GOROOT/bin:$PATH"

# https://github.com/pion/webrtc/tree/master/examples/pion-to-pion
RUN go install github.com/pion/webrtc/v4/examples/pion-to-pion/answer@latest
RUN go install github.com/pion/webrtc/v4/examples/pion-to-pion/offer@latest
# https://github.com/pion/webrtc/tree/master/examples/ortc
RUN go install github.com/pion/webrtc/v4/examples/ortc@latest

# modified ortc2 version
COPY rt /src/rt
RUN go install -C /src/rt

RUN echo "set-option -g mouse on" >> /root/.tmux.conf
RUN echo "export GOROOT=/usr/local/go" >> /root/.profile
RUN echo "export GOPATH=\$HOME/go" >> /root/.profile
RUN echo "export PATH=\$GOPATH/bin:\$GOROOT/bin:\$PATH" >> /root/.profile
RUN echo "alias ta='tmux new -As0'" >> /root/.profile

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
