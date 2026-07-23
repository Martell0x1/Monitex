#!/usr/bin/env bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"

########################################
# Neon-Pastel Color Palette
########################################

# Text Styles
BOLD='\033[1m'
DIM='\033[2m'
ITALIC='\033[3m'

# Colors (256-color palette)
PINK='\033[38;5;213m'     # Soft Neon Pink
PURPLE='\033[38;5;147m'   # Periwinkle / Light Purple
CYAN='\033[38;5;123m'     # Soft Cyan
TEAL='\033[38;5;86m'      # Mint / Emerald Teal
LAVENDER='\033[38;5;183m' # Lavender Accent
RED='\033[38;5;203m'      # Pastel Red
NC='\033[0m'              # No Color

########################################
# UI Helpers
########################################

log() {
    echo -e "${LAVENDER}│${NC} $1"
}

step() {
    echo -e "\n${BOLD}${CYAN}◆ ${NC}${BOLD}$1${NC}"
}

success() {
    echo -e "${LAVENDER}┗━━${NC} ${TEAL}✔ $1${NC}"
}

warn() {
    echo -e "${LAVENDER}┗━━${NC} ${PURPLE}⚠ $1${NC}"
}

error() {
    echo -e "${LAVENDER}┗━━${NC} ${RED}✖ $1${NC}"
}

########################################
# Config
########################################

INSTALL_DIR="/opt/monitex"
DISTRO=""

########################################
# Fixed ASCII Banner
########################################

banner() {
    clear
    echo -e "${PINK}"
    # Single quotes around 'EOF' treat the text exactly as it looks
    cat << 'EOF'
           .==-.                   .-==.
            \()8`-._  `.   .`  _.-8()/
            (88"   ::.  \./  .::   "88)
             \_.'`-::::.(#).::::-'\_/
               `._... .q(_)p. ..._.
                 ""-..-'|=|`-..-""
EOF
    echo -e "${NC}"
    echo -e "  ${BOLD}${PURPLE}M O N I T E X${NC} ${DIM}v2.0.4${NC}"
    echo -e "  ${DIM}IoT Monitoring Ecosystem & Edge Setup${NC}"
    echo -e "${DIM}───────────────────────────────────────────────${NC}"
    echo ""
}

########################################
# Detect distro
########################################

detect_distro() {
    step "Environment Discovery"
    log "Probing Linux distribution..."

    if [ -f /etc/debian_version ]; then
        DISTRO="debian"
    elif [ -f /etc/arch-release ]; then
        DISTRO="arch"
    elif [ -f /etc/fedora-release ]; then
        DISTRO="fedora"
    else
        error "Unsupported Linux distribution"
        exit 1
    fi

    success "Platform identified: ${BOLD}$DISTRO${NC}"
}

########################################
# Hostname
########################################

install_hostname_command(){
    case "$DISTRO" in 
        debian)
            sudo apt update > /dev/null
            sudo apt install inetutils > /dev/null
            ;;
        arch)
            sudo -Sy --noconfirm inetutils > /dev/null
            ;;
        fedora)
            sudo dnf install -y inetutils > /dev/null
            ;;
    esac
    log "Success inetutils insatlled"

}

setup_hostname() {
    step "Network Identity"

    # Ensure hostnamectl is available
    if ! command -v hostnamectl >/dev/null 2>&1; then
        log "hostnamectl not found. Installing..."
        install_hostname_command
    fi

    CURRENT=$(hostnamectl | grep "Static hostname" | grep -oP ":\s*\K.*")

    if [ "$CURRENT" != "monitex" ]; then
        log "Updating system hostname..."
        sudo hostnamectl set-hostname monitex
    fi

    success "Node identity set to 'monitex'"
}
########################################
# Install Avahi
########################################

install_avahi() {
    step "Local DNS (mDNS)"

    if command -v avahi-daemon >/dev/null; then
        success "Avahi is already operational"
    else
        log "Installing networking components..."
        case "$DISTRO" in
            debian)
                sudo apt update > /dev/null
                sudo apt install -y avahi-daemon libnss-mdns > /dev/null
                ;;
            arch)
                sudo pacman -Sy --noconfirm avahi nss-mdns > /dev/null
                ;;
            fedora)
                sudo dnf install -y avahi avahi-tools > /dev/null
                ;;
        esac
        success "Installation complete"
    fi

    log "Enabling background daemon..."
    sudo systemctl enable avahi-daemon &>/dev/null
    sudo systemctl start avahi-daemon &>/dev/null

    if systemctl is-active --quiet avahi-daemon; then
        success "Avahi daemon active"
    else
        error "Avahi failed to initialize"
        exit 1
    fi
}

########################################
# Install Docker
########################################

install_docker() {
    step "Container Runtime"

    if command -v docker >/dev/null; then
        success "Docker engine detected"
        return
    fi

    log "Deploying Docker via official script..."
    curl -fsSL https://get.docker.com | sh > /dev/null

    sudo systemctl enable docker &>/dev/null
    sudo systemctl start docker &>/dev/null
    sudo usermod -aG docker "$USER"

    success "Docker engine active"
}

########################################
# Check Docker Compose
########################################

check_compose() {
    step "Orchestration"
    if docker compose version >/dev/null 2>&1; then
        success "Docker Compose ready"
    else
        error "Compose V2 is missing"
        exit 1
    fi
}

########################################
# Deploy stack
########################################
deploy_stack() {
    step "Stack Deployment"

    cd "$REPO_DIR"

    # Services defined in docker-compose.yml
    mapfile -t expected_services < <(docker compose config --services)

    # Currently running services
    mapfile -t running_services < <(docker compose ps --status running --services)

    missing_services=()

    for service in "${expected_services[@]}"; do
        if ! printf '%s\n' "${running_services[@]}" | grep -qx "$service"; then
            missing_services+=("$service")
        fi
    done

    if [ ${#missing_services[@]} -eq 0 ]; then
        success "All services are running"
    else
        log "Missing/stopped services: ${missing_services[*]}"
        log "Deploying Monitex stack..."
        docker compose up --build -d
        success "Monitex stack deployed"
    fi
}
########################################
# Simulator
########################################
run_simulator() {
    step "Simulator Discovery"
    EDGE_DIR="edge"

    if [ ! -d "$EDGE_DIR" ]; then
        error "Directory 'edge/' not found"
        return
    fi

    simulators=()
    while IFS= read -r -d '' file; do
        simulators+=("$file")
    done < <(find "$EDGE_DIR" -type f \( -name "*.sh" -o -name "*.py" -o -name "*.js" \) -print0)

    if [ ${#simulators[@]} -eq 0 ]; then
        warn "No scripts found"
        return
    fi

    echo -e "\n  ${DIM}Select a simulator:${NC}"
    for i in "${!simulators[@]}"; do
        printf "  ${PINK}%d)${NC} %s\n" "$((i+1))" "${simulators[$i]}"
    done
    echo ""

    read -rp "  Select > " choice

    index=$((choice-1))
    if [[ ! "$choice" =~ ^[0-9]+$ ]] || [ "$index" -ge "${#simulators[@]}" ]; then
        error "Invalid selection"
        return
    fi

    simulator="${simulators[$index]}"
    log "Executing ${BOLD}$simulator${NC}..."

    case "$simulator" in
        *.sh)
            chmod +x "$simulator"
            "$simulator" &
            ;;
        *.py)
            python3 "$simulator" &
            ;;
        *.js)
            node "$simulator" &
            ;;
        *)
            warn "Unsupported simulator type"
            return
            ;;
    esac

    success "Simulator backgrounded"
}

########################################
# Edge mode selection
########################################

edge_mode() {
    echo -e "\n${BOLD}${CYAN}◆ Edge Configuration${NC}"
    echo -e "  ${PINK}1)${NC} Real ESP32 Hardware"
    echo -e "  ${PINK}2)${NC} Software Simulator"
    echo ""
    read -rp "  Select > " choice

    case "$choice" in
        1)
            success "Targeting: ${BOLD}mqtt.monitex.local${NC}"
            ;;
        2)
            run_simulator
            ;;
        *)
            warn "Invalid choice"
            edge_mode
            ;;
    esac
}

########################################
# Health Check
########################################

health_check() {
    step "Final Diagnostics"

    if docker ps | grep monitex >/dev/null; then
        success "Containers: Healthy"
    else
        warn "Containers: Check required"
    fi

    if ping -c 1 monitex.local >/dev/null 2>&1; then
        success "Networking: Verified"
    else
        warn "Networking: Resolution issue"
    fi
}

########################################
# Remove stack (containers + images)
########################################

remove_stack() {
    step "Stack Teardown"

    if ! command -v docker >/dev/null 2>&1; then
        error "Docker is not installed"
        return 1
    fi

    if ! docker compose version >/dev/null 2>&1; then
        error "Compose V2 is missing"
        return 1
    fi

    cd "$REPO_DIR"

    if [ ! -f "$REPO_DIR/docker-compose.yml" ]; then
        error "docker-compose.yml not found in ${BOLD}$REPO_DIR${NC}"
        return 1
    fi

    echo -e "\n  ${DIM}This will stop and remove:${NC}"
    echo -e "  ${PINK}•${NC} All Monitex compose containers"
    echo -e "  ${PINK}•${NC} Their associated Docker images"
    echo -e "  ${DIM}Volumes and host data are left untouched.${NC}"
    echo ""
    read -rp "  Confirm teardown (y/N) > " confirm

    case "$confirm" in
        y|Y|yes|YES)
            ;;
        *)
            warn "Teardown cancelled"
            return 0
            ;;
    esac

    mapfile -t services < <(docker compose config --services 2>/dev/null || true)
    mapfile -t images < <(docker compose config --images 2>/dev/null || true)
    mapfile -t running < <(docker compose ps -q 2>/dev/null || true)

    if [ ${#services[@]} -gt 0 ]; then
        log "Compose services: ${BOLD}${services[*]}${NC}"
    fi

    if [ ${#images[@]} -gt 0 ]; then
        log "Tracked images:"
        for img in "${images[@]}"; do
            echo -e "  ${DIM}→${NC} $img"
        done
    fi

    if [ ${#running[@]} -eq 0 ]; then
        warn "No Monitex containers currently running"
    else
        log "Stopping ${BOLD}${#running[@]}${NC} container(s)..."
    fi

    log "Removing containers, networks, and images..."
    if docker compose down --rmi all --remove-orphans; then
        success "Compose stack removed"
    else
        error "Compose teardown reported errors"
        return 1
    fi

    # Clean leftover monitex-* containers (stopped or orphaned)
    mapfile -t leftover < <(docker ps -aq --filter "name=monitex-" 2>/dev/null || true)
    if [ ${#leftover[@]} -gt 0 ]; then
        log "Removing leftover monitex containers..."
        docker rm -f "${leftover[@]}" >/dev/null 2>&1 || true
        success "Leftover containers cleared"
    fi

    # Remove any remaining images that still match compose image refs
    removed_images=0
    for img in "${images[@]}"; do
        if docker image inspect "$img" >/dev/null 2>&1; then
            log "Removing image ${BOLD}$img${NC}..."
            if docker rmi -f "$img" >/dev/null 2>&1; then
                removed_images=$((removed_images + 1))
            else
                warn "Could not remove image: $img"
            fi
        fi
    done

    if [ "$removed_images" -gt 0 ]; then
        success "Removed ${BOLD}$removed_images${NC} remaining image(s)"
    fi

    success "Monitex containers and images purged"
}

########################################
# Install flow
########################################

install_flow() {
    detect_distro
    setup_hostname
    install_avahi
    install_docker
    check_compose
    deploy_stack
    edge_mode
    health_check

    echo -e "\n${DIM}───────────────────────────────────────────────${NC}"
    echo -e "${TEAL}${BOLD}  Setup Complete!${NC}"
    echo -e "  Dashboard Available at: ${BOLD}${CYAN}http://monitex.local${NC}"
    echo -e "${DIM}───────────────────────────────────────────────${NC}\n"
}

########################################
# Main menu
########################################

main_menu() {
    echo -e "${BOLD}${CYAN}◆ Main Menu${NC}"
    echo -e "  ${PINK}1)${NC} Install / Deploy Monitex"
    echo -e "  ${PINK}2)${NC} Remove Monitex containers & images"
    echo -e "  ${PINK}3)${NC} Exit"
    echo ""
    read -rp "  Select > " choice

    case "$choice" in
        1)
            install_flow
            ;;
        2)
            remove_stack
            echo -e "\n${DIM}───────────────────────────────────────────────${NC}"
            echo -e "${TEAL}${BOLD}  Teardown Complete${NC}"
            echo -e "  Run option ${BOLD}1${NC} again to redeploy the stack."
            echo -e "${DIM}───────────────────────────────────────────────${NC}\n"
            ;;
        3)
            success "Exiting installer"
            exit 0
            ;;
        *)
            warn "Invalid choice"
            echo ""
            main_menu
            ;;
    esac
}

########################################
# MAIN
########################################

banner
main_menu
