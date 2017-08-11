using System;
using System.Collections.Generic;

namespace LegendServer.Util
{
    //随机数
    public static class MyRandom
    {
        private static readonly Random random = new Random();

        // 摘要:
        //     返回非负随机数。
        //
        // 返回结果:
        //     大于等于零且小于 System.Int32.MaxValue 的 32 位带符号整数。
        public static int Next()
        {
            return random.Next();
        }
        //
        // 摘要:
        //     返回一个小于所指定最大值的非负随机数。
        //
        // 参数:
        //   maxValue:
        //     要生成的随机数的上限（随机数不能取该上限值）。maxValue 必须大于或等于零。
        //
        // 返回结果:
        //     大于等于零且小于 maxValue 的 32 位带符号整数，即：返回值的范围通常包括零但不包括 maxValue。不过，如果 maxValue 等于零，则返回
        //     maxValue。
        //
        // 异常:
        //   System.ArgumentOutOfRangeException:
        //     maxValue 小于零。
        public static int Next(int maxValue)
        {
            return random.Next(maxValue);
        }
        //
        // 摘要:
        //     返回一个指定范围内的随机数。
        //
        // 参数:
        //   minValue:
        //     返回的随机数的下界（随机数可取该下界值）。
        //
        //   maxValue:
        //     返回的随机数的上界（随机数不能取该上界值）。maxValue 必须大于或等于 minValue。
        //
        // 返回结果:
        //     一个大于等于 minValue 且小于 maxValue 的 32 位带符号整数，即：返回的值范围包括 minValue 但不包括 maxValue。如果
        //     minValue 等于 maxValue，则返回 minValue。
        //
        // 异常:
        //   System.ArgumentOutOfRangeException:
        //     minValue 大于 maxValue。
        public static int Next(int minValue, int maxValue)
        {
            try
            {
                return random.Next(minValue, maxValue);
            }
            catch (System.Exception)
            {
                return minValue;
            }

        }
        //
        // 摘要:
        //     返回一个相对高精确的指定范围内的随机数。
        //
        // 参数:
        //   minValue:
        //     返回的随机数的下界（随机数可取该下界值）。
        //
        //   maxValue:
        //     返回的随机数的上界（随机数不能取该上界值）。maxValue 必须大于或等于 minValue。
        //
        // 返回结果:
        //     一个大于等于 minValue 且小于 maxValue 的 32 位带符号整数，即：返回的值范围包括 minValue 但不包括 maxValue。如果
        //     minValue 等于 maxValue，则返回 minValue。
        //
        // 异常:
        //   System.ArgumentOutOfRangeException:
        //     minValue 大于 maxValue。
        public static int NextPrecise(int minValue, int maxValue)
        {
            try
            {
                return new Random(Guid.NewGuid().GetHashCode()).Next(minValue, maxValue);
            }
            catch (System.Exception)
            {
                return minValue;
            }

        }
        //
        // 摘要:
        //     用随机数填充指定字节数组的元素。
        //
        // 参数:
        //   buffer:
        //     包含随机数的字节数组。
        //
        // 异常:
        //   System.ArgumentNullException:
        //     buffer 为 null。
        public static void NextBytes(byte[] buffer)
        {
            try
            {
                random.NextBytes(buffer);
            }
            catch (System.Exception)
            {
                return;
            }
        }
        //
        // 摘要:
        //     返回一个介于 0.0 和 1.0 之间的随机数。
        //
        // 返回结果:
        //     大于等于 0.0 并且小于 1.0 的双精度浮点数。
        public static double NextDouble()
        {
            return random.NextDouble();
        }
        //
        // 摘要:
        //     返回一个相对高精确的介于 0.0 和 1.0 之间的随机数。
        //
        // 返回结果:
        //     大于等于 0.0 并且小于 1.0 的双精度浮点数。
        public static double NextPreciseDouble()
        {
            return new Random(Guid.NewGuid().GetHashCode()).NextDouble();
        }
        /// <summary>
        /// 权重随机盒,每个元素带有自己的权重
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class PowerRandomBox<T>
        {
            //权重信息
            private class PowerInfo
            {
                public int Original;//原始
                public int Actual;//实际
                public PowerInfo(int original, int actual)
                {
                    this.Original = original;
                    this.Actual = actual;
                }
            }
            private int actualPower;
            private List<Tuple<T, PowerInfo>> elements = new List<Tuple<T, PowerInfo>>();

            public void AddElement(T element, int power)
            {
                if (power < 0) return;

                actualPower += power;
                elements.Add(new Tuple<T, PowerInfo>(element, new PowerInfo(power, actualPower)));
            }

            public T Next(bool popup = false/**被抽中后是否弹出来并重新计算剩余的元素的实际权重*/)
            {
                Tuple<T, PowerInfo> result = null;
                int preActualPower = 0;
                int rand = MyRandom.Next(1, actualPower + 1);

                for (int index = 0; index < elements.Count; index++)
                {
                    Tuple<T, PowerInfo> element = elements[index];

                    if (result == null)
                    {
                        if (element.Item2.Actual >= rand)
                        {
                            if (!popup)
                            {
                                //不弹出已找到的则直接返回
                                return element.Item1;
                            }
                            else
                            {
                                //标记即将弹出的已找到的元素
                                result = element;
                                //记录该元素的上一个元素的真实权重
                                preActualPower = index > 0 ? elements[index - 1].Item2.Actual : 0;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        //将剩下的元素重新计算权重
                        element.Item2.Actual = preActualPower + element.Item2.Original;
                        preActualPower = element.Item2.Actual;
                    }
                }

                if (popup)
                {
                     if (result != null)
                     {
                         //弹出被选中的元素
                         elements.Remove(result);

                         if (elements.Count > 0)
                         {
                             //重新记录最大真实权重
                             actualPower = elements[elements.Count - 1].Item2.Actual;
                         }

                         return result.Item1;
                     }
                     else
                     {
                         return default(T);
                     }
                }
                else
                {
                    return default(T);
                }
            }
        }
        /// <summary>
        /// 概率随机盒,每个元素带有自己独立的概率，相互之间不影响
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class ProbabilityRandomBox<T>
        {
            private List<Tuple<T, int>> elements = new List<Tuple<T, int>>();

            public void AddElement(T element, int probability)
            {
                if (probability < 0) return;

                elements.Add(new Tuple<T, int>(element, probability));
            }

            public List<T> Next()
            {
                List<T> result = new List<T>();

                for (int index = 0; index < elements.Count; index++)
                {
                    Tuple<T, int> element = elements[index];

                    int rand = MyRandom.Next(1, 101);
                    if (element.Item2 >= rand)
                    {
                        result.Add(element.Item1);
                    }                    
                }

                return result;
            }
        }
    }

}
