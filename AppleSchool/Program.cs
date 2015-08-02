using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Globalization;
using System.Xml;

namespace AppleSchool
{
    class Program
    {
        /// <summary>
        /// Начало программы
        /// </summary>
        /// <param name="args">Переданные аргументы</param>
        static void Main(string[] args)
        {
            // Проверяем что нам передали в параметрах файл для анализа
            if (args.Length < 1)
            {
                // Если нет - выводим сообщенеи как пользоваться прогой
                Console.WriteLine("Usage: AppleSchool.exe <path_to_xml>");

                // Ждем нажатия любой клавиши
                PressAnyKey();

                // Выходим из программы
                return;
            }

            // Проверям что переданный в параметрах файл существует
            if (!File.Exists(args[0]))
            {
                // Если нет - сообщение, клавиша, выход
                Console.WriteLine("File {0} not found", args[0]);
                PressAnyKey();
                return;
            }

            // Загружаем файл для анализа
            try
            {
                XDocument xdoc = XDocument.Load(args[0]);

                // Суммы по типам товаров
                Dictionary<string, Decimal> summs = new Dictionary<string, decimal>();

                // Суммы по заказам
                Dictionary<string, Tuple<decimal, decimal>> orders = new Dictionary<string, Tuple<decimal, decimal>>();

                // Сумма для типа Прочие (без типа или пустые типы)
                decimal summOther = 0m;
                // Итого
                decimal summTotal = 0m;

                // Вычислем суммы
                Calculation(xdoc, summs, orders, ref summOther, ref summTotal);

                // Выводим суммы по типам 
                DisplaySumm(summs, summOther, summTotal);

                

                // Выводим разницы если есть
                DisplayDiff(orders);
            } catch ( XmlException ex ) {

                // Если была ошибка при разборе файла - сообщаем
                Console.WriteLine("Ошибка при обработке переданного файла");
            }

            // Ждем Any Key и выходим
            PressAnyKey();

        }

        /// <summary>
        /// Приглашение к нажатию любой клавиши и ожиданеи нажатия
        /// </summary>
        private static void PressAnyKey()
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        /// <summary>
        /// Выводим разницы сумм в заказе и по товарам, если таковые есть
        /// </summary>
        /// <param name="orders">Заказы с суммами</param>
        private static void DisplayDiff(Dictionary<string, Tuple<decimal, decimal>> orders)
        {
            // Выводим первый раз
            bool first = true;

            // Формат вывода
            const string formatDiff = "{0,-15}{1,-15}{2}";

            // Для всех заказов где сумма в заказе отличается от суммы по товарам
            foreach (var order in orders.Where(x => x.Value.Item1 != x.Value.Item2))
            {
                // если вывод первый раз 
                if (first)
                {
                    // Выводим "шапку"
                    Console.WriteLine();
                    Console.WriteLine("Расхождение суммы по товарам с указанной в заказе");
                    Console.WriteLine(formatDiff, "Заказ", "Сумма в заказе", "Сумма по товарам");

                    // Ставим флаг что "шапку" вывели
                    first = false;
                }
                
                //Выводим заказ и суммы
                Console.WriteLine(formatDiff, order.Key, order.Value.Item1, order.Value.Item2);
            }
        }

        /// <summary>
        ///  Выводим суммы по типам товаров
        /// </summary>
        /// <param name="summs">Суммы по типам товаров</param>
        /// <param name="summOther">Сумма для типа Прочие (без типа или пустые типы)</param>
        /// <param name="summTotal">Итого</param>
        private static void DisplaySumm(Dictionary<string, decimal> summs, decimal summOther, decimal summTotal)
        {
            // Формат вывода
            const string format = "{0,-15}{1}";

            // Выводим "шапку"
            Console.WriteLine("Сумма по типам");
            Console.WriteLine(format, "Type", "Total");

            //Для каждого типа
            foreach (var pair in summs)
            {
                // Выводим сумму
                Console.WriteLine(format, pair.Key, pair.Value);
            }
            // Сумма для прочих
            Console.WriteLine(format, "Прочее", summOther);
            // Итого
            Console.WriteLine(format, "Итого:", summTotal);

        }

        /// <summary>
        /// Расчет сумм
        /// </summary>
        /// <param name="xdoc">XML Документ для анализа</param>
        /// <param name="summs">Суммы по типам товаров</param>
        /// <param name="orders">Суммы по заказам</param>
        /// <param name="summOther">Сумма типа Прочее</param>
        /// <param name="summTotal">Итого</param>
        private static void Calculation(XDocument xdoc, Dictionary<string, Decimal> summs, Dictionary<string, Tuple<decimal, decimal>> orders, ref decimal summOther, ref decimal summTotal)
        {
            // Для всех заказов в Xml документе
            foreach (var order in xdoc.Descendants("order"))
            {
                // Получаем ID заказа
                XAttribute xid = order.Attribute("id");
                string id = xid != null && !String.IsNullOrWhiteSpace(xid.Value) ? xid.Value : "0";

                // Получаем сумму в заказа
                XAttribute xtotal = order.Attribute("totalPrice");
                decimal totalPrice = (xtotal != null) ? TryConvert(xtotal.Value, 0M) : 0m;

                // Сумма товаров в заказе
                decimal itemSumm = 0M;

                // Для всех товаров в заказе
                foreach (var item in order.Descendants("item"))
                {
                    // Получаем цену
                    XAttribute xprice = item.Attribute("price");
                    decimal price = (xprice != null) ? TryConvert(xprice.Value, 0m) : 0m;

                    // Если цена не 0
                    if (price != 0m)
                    {
                        // Получаем тип товара
                        XAttribute xtype = item.Attribute("type");
                        
                        if (xtype != null && !String.IsNullOrWhiteSpace(xtype.Value))
                        {
                            // Если тип существует и не пустой
                            if (!summs.ContainsKey(xtype.Value))
                            {
                                // Если такого типа еще нет в словаре - добавляем с ценой товара
                                summs[xtype.Value] = price;
                            }
                            else
                            {
                                // Если тип есть - добавляем цену товара к существующей сумме
                                summs[xtype.Value] += price;
                            }
                        }
                        else
                        {
                            // Если тип не указан - добавляем цену к сумме по типу Прочие
                            summOther += price;
                        }
                        // Добавляем цену товара с сумме Итого
                        summTotal += price;
                        // Добвляем цену товара к сумме заказа
                        itemSumm += price;
                    }
                }

                // заносим в список заказов новую запись с суммой в зказе и сммой по товарам
                orders[id] = new Tuple<decimal, decimal>(totalPrice, itemSumm);
            }
        }

        
        /// <summary>
        /// Переменная отвечает какую конвертацию использовать первой - с учетом региональных настройек или без
        /// </summary>
        private static bool _TryInvariantFirst = false;

        /// <summary>
        /// Получает Decimal из String.
        /// </summary>
        /// <param name="strValue">Строка со значением</param>
        /// <param name="defaultValue">значенеи по умолчанию если преобразовать строку не удалось</param>
        /// <returns>Полученное значение Decimal</returns>
        private static decimal TryConvert(string strValue, decimal defaultValue)
        {

            // Если строка пустая - возвращаем значенеи по умолчанию
            if (String.IsNullOrWhiteSpace(strValue))
            {
                return defaultValue;
            }

            // Результат
            decimal result = defaultValue;

            // Если вначале преобразуем через InvarianInfo
            if (_TryInvariantFirst)
            {
                // Если получилось преобразовать
                if (decimal.TryParse(strValue, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out result))
                    // Выходим
                    return result;

            }

            //Пробуем преобразовать с текущими региональными настройками
            if (decimal.TryParse(strValue, out result))
            {
                //Получилось - выходим
                return result;
            }

            // Пробуем через InvarianInfo
            if (decimal.TryParse(strValue, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out result))
                // Получилось - ставм флаг что в следующий раз пробовать вначале через InvarianInfo
                _TryInvariantFirst = true;

            // Возвращаем результат
            return result;
        }
    }
}
