using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModbusCrcCalculator
{
    public partial class MainWindow : Window
    {
        private bool isUpdatingConverter = false;

        // Полная таблица CRC16 для алгоритма MODBUS
        private static readonly ushort[] crc16Table = new ushort[]
        {
            0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
            0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
            0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
            0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
            0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
            0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
            0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
            0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
            0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
            0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
            0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
            0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
            0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
            0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
            0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
            0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
            0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
            0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
            0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
            0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
            0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
            0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
            0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
            0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
            0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
            0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
            0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
            0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
            0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
            0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
            0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
            0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
        };

        public MainWindow()
        {
            InitializeComponent();
            CalculateCrc(); // Вычисляем CRC при запуске
        }

        private void OnInputLostFocus(object sender, RoutedEventArgs e)
        {
            // Синхронизируем поля ввода с общим полем hex-строки
            if (sender != HexStringTextBox)
            {
                UpdateHexStringFromFields();
            }
            else
            {
                UpdateFieldsFromHexString();
            }

            CalculateCrc();
        }

        private void UpdateHexStringFromFields()
        {
            if (DeviceAddressTextBox == null || CommandTextBox == null ||
                RegisterAddressTextBox == null || RegisterCountTextBox == null ||
                HexStringTextBox == null)
                return;

            var hexString = $"{DeviceAddressTextBox.Text} {CommandTextBox.Text} {RegisterAddressTextBox.Text} {RegisterCountTextBox.Text}";
            HexStringTextBox.Text = hexString.Trim();
        }

        private void UpdateFieldsFromHexString()
        {
            if (DeviceAddressTextBox == null || CommandTextBox == null ||
                RegisterAddressTextBox == null || RegisterCountTextBox == null ||
                HexStringTextBox == null)
                return;

            var parts = HexStringTextBox.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1) DeviceAddressTextBox.Text = parts[0];
            if (parts.Length >= 2) CommandTextBox.Text = parts[1];
            if (parts.Length >= 3) RegisterAddressTextBox.Text = parts.Length >= 4 ? $"{parts[2]} {parts[3]}" : parts[2];
            if (parts.Length >= 5) RegisterCountTextBox.Text = parts.Length >= 6 ? $"{parts[4]} {parts[5]}" : parts[4];
        }

        private void CalculateCrc()
        {
            if (HexStringTextBox == null || ResultTextBox == null)
                return;

            try
            {
                var hexString = HexStringTextBox.Text;
                if (string.IsNullOrWhiteSpace(hexString))
                {
                    ResultTextBox.Text = "Input hex string";
                    if (FullStringLabel != null)
                        FullStringLabel.Content = "Press to copy in clipboard";
                    return;
                }

                var bytes = ParseHexString(hexString);

                if (bytes.Length == 0)
                {
                    ResultTextBox.Text = "Введите корректную hex-строку";
                    if (FullStringLabel != null)
                        FullStringLabel.Content = "Нажмите для копирования полной строки";
                    return;
                }

                var crc = CalculateModbusCrc16(bytes);
                // Меняем байты местами для MODBUS формата (Little Endian)
                var swappedCrc = (ushort)((crc << 8) | (crc >> 8));
                ResultTextBox.Text = $"CRC: 0x{swappedCrc:X4}";

                // Обновляем полную строку
                UpdateFullString(bytes, swappedCrc);
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"Ошибка: {ex.Message}";
            }
        }

        private byte[] ParseHexString(string hexString)
        {
            // Удаляем пробелы и проверяем корректность
            var cleanHex = Regex.Replace(hexString, @"\s+", "");

            if (cleanHex.Length % 2 != 0)
                throw new ArgumentException("Check input fields");

            var bytes = new byte[cleanHex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(cleanHex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }

            return bytes;
        }

        private ushort CalculateModbusCrc16(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                byte tableIndex = (byte)(crc ^ b);
                crc >>= 8;
                crc ^= crc16Table[tableIndex];
            }

            return crc;
        }

        private void UpdateFullString(byte[] data, ushort crc)
        {
            if (FullStringLabel == null)
                return;

            var hexString = string.Join(" ", data.Select(b => b.ToString("X2")));
            // В полной строке показываем CRC в том же порядке что и в результате (поменянные байты)
            var crcLow = (byte)(crc & 0xFF);
            var crcHigh = (byte)(crc >> 8);
            var fullString = $"{hexString} {crcHigh:X2} {crcLow:X2}";

            FullStringLabel.Content = $"Full command: {fullString}";
        }

        private void OnFullStringClick(object sender, MouseButtonEventArgs e)
        {
            if (FullStringLabel?.Content == null)
                return;

            try
            {
                var content = FullStringLabel.Content.ToString();
                var fullString = content.Replace("Full command: ", "");

                Clipboard.SetText(fullString);
                ShowStatus("Command copied into clipboard!");
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка копирования: {ex.Message}");
            }
        }

        private void ShowStatus(string message)
        {
            if (StatusTextBlock == null)
                return;

            StatusTextBlock.Text = message;

            // Очищаем статус через 3 секунды
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                StatusTextBlock.Text = "";
                timer.Stop();
            };
            timer.Start();
        }

        private void OnDecimalTextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingConverter) return;

            try
            {
                isUpdatingConverter = true;

                if (string.IsNullOrWhiteSpace(DecimalTextBox.Text))
                {
                    HexTextBox.Text = "";
                    return;
                }

                if (int.TryParse(DecimalTextBox.Text, out int decimalValue))
                {
                    HexTextBox.Text = decimalValue.ToString("X");
                }
                else
                {
                    HexTextBox.Text = "Invalid";
                }
            }
            catch
            {
                HexTextBox.Text = "Error";
            }
            finally
            {
                isUpdatingConverter = false;
            }
        }

        private void OnHexTextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingConverter) return;

            try
            {
                isUpdatingConverter = true;

                if (string.IsNullOrWhiteSpace(HexTextBox.Text))
                {
                    DecimalTextBox.Text = "";
                    return;
                }

                if (int.TryParse(HexTextBox.Text, NumberStyles.HexNumber, null, out int hexValue))
                {
                    DecimalTextBox.Text = hexValue.ToString();
                }
                else
                {
                    DecimalTextBox.Text = "Invalid";
                }
            }
            catch
            {
                DecimalTextBox.Text = "Error";
            }
            finally
            {
                isUpdatingConverter = false;
            }
        }
    }
}
