# ChatServer
C# WebSocket ChatServer

Linux Setting
apt-get install update
apt-get install apt-transport-https
apt-get install -ca-certificates
apt-get install curl -Y
apt-get install software-properties-common -Y

apt-get install docker.io
apt-get install git

curl -L "https://github.com/docker/compose/releases/download/1.28.5/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
ln -s /usr/local/bin/docker-compose /usr/bin/docker-compose
//설치 확인
docker-compose --version  
chmod +x /usr/local/bin/docker-compose 

mkdir ChatServer (채팅서버 생성할곳은 /home/ubuntu/ 경로에서 생성 권장)
cd ChatServer
git clone (git clone 주소)
docker-compose up -d --build
docker ps (컨테이너에 제대로 서버랑 redis가 올라왔는지확인하면 끝)


