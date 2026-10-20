import { buildNachaUploadFormData } from './nacha-upload.service';

describe('NachaUploadService multipart request', () => {
  const file = new File(['controlled'], '0001283.001.20260731.1.OUT.env', {
    type: 'application/octet-stream'
  });

  it('should omit reprocess fields during a normal upload', () => {
    const form = buildNachaUploadFormData(file, 7);

    expect(form.get('file')).toBe(file);
    expect(form.get('clearingHouseId')).toBe('7');
    expect(form.has('forceReprocess')).toBeFalse();
    expect(form.has('parentIngestionId')).toBeFalse();
  });

  it('should send the explicit canonical parent during controlled reprocess', () => {
    const parentIngestionId = '11111111-1111-1111-1111-111111111111';
    const form = buildNachaUploadFormData(file, 7, {
      forceReprocess: true,
      parentIngestionId
    });

    expect(form.get('forceReprocess')).toBe('true');
    expect(form.get('parentIngestionId')).toBe(parentIngestionId);
  });
});
