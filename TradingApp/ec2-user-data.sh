#!/bin/bash
# Install Docker
yum update -y
amazon-linux-extras install docker -y
service docker start
usermod -a -G docker ec2-user

# Log in to Docker Hub
docker login -u "${DOCKER_USERNAME}" -p "${DOCKER_PASSWORD}"

# Pull and run the Docker containers
docker-compose -f /path/to/your/docker-compose.yml up -d
