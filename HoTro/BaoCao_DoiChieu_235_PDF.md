# Báo cáo Đối chiếu 235 Tệp PDF & Dữ liệu Xuất bản Khoa học
Cơ sở dữ liệu kiểm toán: `QL_TapChiKhoaHoc_MigrateTest` | Thư mục: `Backend\HuitJournal.Api\Uploads\Published`

## 1. Tóm tắt Kiểm toán
- **Tổng số tệp PDF xuất bản trên đĩa:** 235
- **Tổng số bản ghi ThuMucBaiBao (PDF thành phẩm):** 230
- **Số tệp khớp tuyệt đối (100% tồn tại và toàn vẹn):** 230
- **Số tệp trong CSDL bị thiếu trên đĩa:** 0
- **Số tệp mồ côi trên đĩa (không gắn DB):** 5
- **Số tạp chí lịch sử (2016 - 2026):** 23 số
- **Tỷ lệ có ảnh bìa (SVG / JPG):** 23 / 23 (100%)

## 2. Chi tiết 5 Tệp Mồ côi trên đĩa (Orphan Files)
Các tệp sau nằm trên đĩa nhưng không nằm trong danh mục xuất bản chính thức của tòa soạn:

| STT | Tên tệp | Kích thước | Phân loại | Ghi chú & Đối chiếu Hash |
|:---:|:---|:---:|:---|:---|
| 1 | `Published_82b85ac5c4f4_1._Nguyen_Van_Cuong_-_QLKT__1-13_.pdf` | 606,724 bytes | Duplicate Payload | Bản sao trùng hash SHA256 với tệp bài báo chính thức `Published_49b83e222811_1._Nguyen_Van_Cuong_-_QLKT__1-13_.pdf` |
| 2 | `Published_fdb6556d0fca_1._Nguyen_Van_Cuong_-_QLKT__1-13_.pdf` | 606,724 bytes | Duplicate Payload | Bản sao trùng hash SHA256 với tệp bài báo chính thức `Published_49b83e222811_1._Nguyen_Van_Cuong_-_QLKT__1-13_.pdf` |
| 3 | `Published_14_77a8ba82c5cf40579c9bd56ec8daed67_HUIT_Journal_Published_14.pdf` | 51 bytes | Mock Test Artifact | Tệp kiểm thử cục bộ kích thước nhỏ (51B - 78B) từ các đợt chạy thử nghiệm trước |
| 4 | `Published_16_6086fc5c17ca4c3a8ff5e1f376b591b8_Official_Published_Paper_16.pdf` | 78 bytes | Mock Test Artifact | Tệp kiểm thử cục bộ kích thước nhỏ (51B - 78B) từ các đợt chạy thử nghiệm trước |
| 5 | `Published_17_bd39414f83d645229ddf405b9199ec7f_Official_Published_Paper_17.pdf` | 78 bytes | Mock Test Artifact | Tệp kiểm thử cục bộ kích thước nhỏ (51B - 78B) từ các đợt chạy thử nghiệm trước |

## 3. Bảng Kiểm toán Ảnh bìa 23 Số Tạp chí (2016 - 2026)

| Số | Năm | Tên số tạp chí | Bìa SVG Yersin | Bìa JPG HUIT | Trạng thái |
|:---:|:---:|:---|:---:|:---:|:---:|
| 23 | 2026 | Tạp chí Khoa học Yersin - Số 23 (4.2026) | ✓ `cover_yersin_no23.svg` | ✓ `cover_huit_vol1_no23e.jpg` | **OK** |
| 22 | 2025 | Tạp chí Khoa học Yersin - Số 22 (12.2025) | ✓ `cover_yersin_no22.svg` | ✓ `cover_huit_vol1_no22e.jpg` | **OK** |
| 21 | 2025 | Tạp chí Khoa học Yersin - Số 21 (08.2025) | ✓ `cover_yersin_no21.svg` | ✓ `cover_huit_vol1_no21e.jpg` | **OK** |
| 20 | 2025 | Tạp chí Khoa học Yersin - Số 20 (04.2025) | ✓ `cover_yersin_no20.svg` | ✓ `cover_huit_vol1_no20e.jpg` | **OK** |
| 19 | 2024 | Tạp chí Khoa học Yersin - Số 19 (12.2024) | ✓ `cover_yersin_no19.svg` | ✓ `cover_huit_vol1_no19e.jpg` | **OK** |
| 18 | 2024 | Tạp chí Khoa học Yersin - Số 18 (8.2024) | ✓ `cover_yersin_no18.svg` | ✓ `cover_huit_vol1_no18e.jpg` | **OK** |
| 17 | 2024 | Tạp chí Khoa học Yersin - Số 17 (04.2024) | ✓ `cover_yersin_no17.svg` | ✓ `cover_huit_vol1_no17e.jpg` | **OK** |
| 16 | 2023 | Tạp chí Khoa học Yersin - Số 16 (12.2023) | ✓ `cover_yersin_no16.svg` | ✓ `cover_huit_vol1_no16e.jpg` | **OK** |
| 15 | 2023 | Tạp chí Khoa học Yersin - Số 15 (08.2023) | ✓ `cover_yersin_no15.svg` | ✓ `cover_huit_vol1_no15e.jpg` | **OK** |
| 14 | 2023 | Tạp chí Khoa học Yersin - Số 14 (04.2023) | ✓ `cover_yersin_no14.svg` | ✓ `cover_huit_vol1_no14e.jpg` | **OK** |
| 13 | 2022 | Tạp chí Khoa học Yersin - Số 13 (12.2022) | ✓ `cover_yersin_no13.svg` | ✓ `cover_huit_vol1_no13e.jpg` | **OK** |
| 12 | 2022 | Tạp chí Khoa học Yersin - Số 12 (08.2022) | ✓ `cover_yersin_no12.svg` | ✓ `cover_huit_vol1_no12e.jpg` | **OK** |
| 11 | 2022 | Tạp chí Khoa học Yersin - Số 11 (4.2022) | ✓ `cover_yersin_no11.svg` | ✓ `cover_huit_vol1_no11e.jpg` | **OK** |
| 10 | 2021 | Tạp chí Khoa học Yersin - Số 10 (12.2021) | ✓ `cover_yersin_no10.svg` | ✓ `cover_huit_vol1_no10e.jpg` | **OK** |
| 9 | 2021 | Tạp chí Khoa học Yersin - Số 09 (8.2021) | ✓ `cover_yersin_no9.svg` | ✓ `cover_huit_vol1_no9e.jpg` | **OK** |
| 8 | 2020 | Tạp chí Khoa học Yersin - Số 08 (12.2020) | ✓ `cover_yersin_no8.svg` | ✓ `cover_huit_vol1_no8e.jpg` | **OK** |
| 7 | 2020 | Tạp chí Khoa học Yersin - Số 07 (8.2020) | ✓ `cover_yersin_no7.svg` | ✓ `cover_huit_vol1_no7e.jpg` | **OK** |
| 6 | 2019 | Tạp chí Khoa học Yersin - Số 06 (12.2019) | ✓ `cover_yersin_no6.svg` | ✓ `cover_huit_vol1_no6e.jpg` | **OK** |
| 5 | 2019 | Tạp chí Khoa học Yersin - Số 05 (8.2019) | ✓ `cover_yersin_no5.svg` | ✓ `cover_huit_vol1_no5e.jpg` | **OK** |
| 4 | 2019 | Tạp chí Khoa học Yersin - Số 04 (4.2019) | ✓ `cover_yersin_no4.svg` | ✓ `cover_huit_vol1_no4e.jpg` | **OK** |
| 3 | 2017 | Tạp chí Khoa học Yersin - Số 03 (9.2017) | ✓ `cover_yersin_no3.svg` | ✓ `cover_huit_vol1_no3e.jpg` | **OK** |
| 2 | 2017 | Tạp chí Khoa học Yersin - Số 02 (3.2017) | ✓ `cover_yersin_no2.svg` | ✓ `cover_huit_vol1_no2e.jpg` | **OK** |
| 1 | 2016 | Tạp chí Khoa học Yersin - Số 01 (11.2016) | ✓ `cover_yersin_no1.svg` | ✓ `cover_huit_vol1_no1e.jpg` | **OK** |