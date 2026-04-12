import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DownloadComponent } from './download';

describe('DownloadComponent', () => {
  let component: DownloadComponent;
  let fixture: ComponentFixture<DownloadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DownloadComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(DownloadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should present installation guidance', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Install Monitex from the source repository');
    expect(compiled.textContent).toContain('git clone https://github.com/Martell0x1/Monitex.git');
    expect(compiled.textContent).toContain('./installer/installer.sh');
  });
});
