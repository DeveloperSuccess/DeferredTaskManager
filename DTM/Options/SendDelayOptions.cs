using System.ComponentModel.DataAnnotations;

namespace DTM
{
    /// <summary>
    /// Options up the processing of added events after a certain time interval with the 
    /// possibility of variable deducting the time of the previous operation
    /// </summary>
    public class SendDelayOptions
    {
        /// <summary>
        /// The frequency interval for processing the collected data
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        public int MillisecondsSendDelay { get; set; } = 60000;

        /// <summary>
        /// Subtract the time of the previous processing from the delay
        /// </summary>
        [Required]
        public bool ConsiderDifference { get; set; } = false;
    }
}
