#!/bin/bash

# Function to install Docker on Parrot OS (Debian 13 Trixie base)
install_docker_parrot() {
    echo "Installing Docker (Debian 13 Trixie packages) on Parrot OS..."

    # Update package index
    sudo apt update

    # Install required packages
    sudo apt install -y ca-certificates curl

    # Create directory for Docker GPG key
    sudo install -m 0755 -d /etc/apt/keyrings

    # Add Docker's official GPG key
    sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
    sudo chmod a+r /etc/apt/keyrings/docker.asc

    # Set up the stable Docker repository explicitly using 'trixie'
    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian \
      trixie stable" | \
      sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

    # Update package index again with the new repo
    sudo apt update

    # Install Docker packages
    sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Start and enable Docker service
    sudo systemctl start docker
    sudo systemctl enable docker

    echo "Docker installed successfully!"
}

# Add user to the Docker group
add_user_to_docker_group() {
    echo "Adding user to the Docker group..."
    sudo usermod -aG docker $USER
    echo "Please log out and log back in (or run 'newgrp docker') to apply Docker group changes."
}

# Main script logic
install_docker_parrot
add_user_to_docker_group