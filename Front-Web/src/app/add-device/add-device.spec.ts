import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddDevice } from './add-device';

describe('AddDevice', () => {
  let component: AddDevice;
  let fixture: ComponentFixture<AddDevice>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddDevice]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddDevice);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
