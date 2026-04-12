import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

interface InstallStep {
  title: string;
  description: string;
  command: string;
}

interface Requirement {
  name: string;
  description: string;
}

@Component({
  selector: 'app-download',
  imports: [CommonModule, RouterLink],
  templateUrl: './download.html',
  styleUrl: './download.css',
})
export class DownloadComponent {
  protected readonly repoUrl = 'https://github.com/Martell0x1/Monitex.git';

  protected readonly requirements: Requirement[] = [
    {
      name: 'Git',
      description: 'Used to clone the Monitex repository locally before running the installer.',
    },
    {
      name: 'Bash shell',
      description: 'Required to execute the installer script from the repository root.',
    },
    {
      name: 'Network access',
      description: 'The installer may need to reach package registries and service endpoints.',
    },
  ];

  protected readonly installSteps: InstallStep[] = [
    {
      title: 'Clone the repository',
      description: 'Fetch the Monitex source code locally so the installer can be run from the project root.',
      command: `git clone ${this.repoUrl}`,
    },
    {
      title: 'Enter the project directory',
      description: 'Move into the cloned repository before starting the installation script.',
      command: 'cd Monitex',
    },
    {
      title: 'Make the installer executable',
      description: 'If required on your system, grant execute permission to the installer script.',
      command: 'chmod +x ./installer/installer.sh',
    },
    {
      title: 'Start the Monitex installer',
      description: 'Run the installer script from the repository root to begin the Monitex setup flow.',
      command: './installer/installer.sh',
    },
  ];

  protected readonly postInstallNotes = [
    'Keep the terminal open while the installer prepares the local Monitex environment.',
    'Follow any prompts from the script if it asks for service or environment settings.',
    'Once installation completes, open the dashboard and connect your devices or telemetry sources.',
  ];
}
