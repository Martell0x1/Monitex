#!/usr/bin/env bash

set -e

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

setup_hostname() {
    step "Network Identity"
    CURRENT=$(hostname)

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
    
    cd ../ 

    RUNNING=$(docker compose ps --services --filter "status=running" 2>/dev/null | wc -l)
    EXISTING=$(docker compose ps --services 2>/dev/null | wc -l)

    if [ "$EXISTING" -eq 0 ]; then
        log "Initializing fresh stack..."
        docker compose up --build -d
        success "Monitex stack deployed"
    elif [ "$RUNNING" -eq "$EXISTING" ]; then
        success "All services running perfectly"
    else
        log "Restarting dormant services..."
        docker compose up -d
        success "Stack synchronized"
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
# MAIN
########################################

banner
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
