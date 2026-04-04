import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddSensors } from './add-sensors';

describe('AddSensors', () => {
  let component: AddSensors;
  let fixture: ComponentFixture<AddSensors>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddSensors],
    }).compileComponents();

    fixture = TestBed.createComponent(AddSensors);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
