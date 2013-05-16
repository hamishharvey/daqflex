//***************************************************************************************
//
// DAQFlex API Library
//
// Copyright (c) 2009, Measurement Computing Corporation
// All rights reserved
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the MEASUREMENT COMPUTING SOFTWARE LICENSE AGREEMENT (DAQFlex API)
// by Measurement Computing Coporation.
// 
// You should have received a copy of the MEASUREMENT COMPUTING SOFTWARE 
// LICENSE AGREEMENT (DAQFlex API) with this library; If not you can contact
// Measurement Computing Corporation, 10 Commerce Way, Norton MA 02766 USA.
// 
//***************************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace MeasurementComputing.DAQFlex
{
    class SynchronousUsbInterop : UsbPlatformInterop
    {
        protected int m_bulkXferSize;
        protected uint m_inputXferTimeOut;
        protected bool m_dataOverrunOccurred;
        protected Thread m_processBulkInRequests;
        protected bool m_bulkInRequestsStarted = false;
        protected Thread m_processBulkOutRequests;
        protected bool m_bulkOutRequestsStarted = false;

        //=====================================================================================
        /// <summary>
        /// Default constructor used by the daq device manager before devices are detected
        /// </summary>
        //=====================================================================================
        internal SynchronousUsbInterop()
        {
        }

        //=====================================================================================
        /// <summary>
        /// Device-specific constructor used by the driver interface
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        //=====================================================================================
        internal SynchronousUsbInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
        {
			m_readyToStartOutputScan = false;
        }

        //=====================================================================================================
        /// <summary>
        /// Overrides abstract method in base class
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //=====================================================================================================
        internal override ErrorCodes GetUsbDevices(Dictionary<int, DeviceInfo> deviceInfoList, bool refresh)
        {
            System.Diagnostics.Debug.Assert(false, "GetUsbDevices not implemented in SynchronousUsbInterop");
            return ErrorCodes.MethodRequiresImplementation;
        }

        protected static object bulkInRequestLock = new object();

        //======================================================================================
        /// <summary>
        /// Indicates if a bulk read has been submitted prior to the device actually starting
        /// </summary>
        //======================================================================================
        protected bool BulkInRequestsStarted
        {
            get
            {
                lock (bulkInRequestLock)
                {
                    return m_bulkInRequestsStarted;
                }
            }

            set
            {
                lock (bulkInRequestLock)
                {
                    m_bulkInRequestsStarted = value;
                }
            }
        }

        protected static object bulkOutRequestLock = new object();

        //======================================================================================
        /// <summary>
        /// Indicates if a bulk read has been submitted prior to the device actually starting
        /// </summary>
        //======================================================================================
        protected bool BulkOutRequestsStarted
        {
            get
            {
                lock (bulkOutRequestLock)
                {
                    return m_bulkOutRequestsStarted;
                }
            }

            set
            {
                lock (bulkOutRequestLock)
                {
                    m_bulkOutRequestsStarted = value;
                }
            }
        }
        //===================================================================================================
        /// <summary>
        /// Creates one or more BulkInRequest objects that contain the overlapped struct and data buffer
        /// These are used by the SubmitBulkInRequest and CompleteBulkInRequest methods
        /// </summary>
        //===================================================================================================
        protected virtual void CreateBulkInRequestObjects()
        {
        }

        //===================================================================================================
        /// <summary>
        /// Creates one or more BulkInRequest objects that contain the overlapped struct and data buffer
        /// These are used by the SubmitBulkInRequest and CompleteBulkInRequest methods
        /// </summary>
        //===================================================================================================
        protected virtual void CreateBulkOutRequestObjects()
        {
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method that allows a derived platform interop object to
        /// set up parameters for data transfer
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //===========================================================================================
        internal override void PrepareInputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            BulkInRequestsStarted = false;

            m_errorCode = ErrorCodes.NoErrors;

            m_inputScanTriggered = false;

            m_completedBulkInRequestBuffers.Clear();

            m_bulkXferSize = transferSize;

            if (m_inputSampleMode == SampleMode.Finite)
            {
                m_numberOfWorkingInputRequests = 1;

                if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
                {
                    int aiChannelCount = m_criticalParams.AiChannelCount;
                    int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

                    TotalNumberOfInputRequests = totalNumberOfBytes / (byteRatio * aiChannelCount);
                }
                else
                {
                    if (totalNumberOfBytes <= transferSize)
                        TotalNumberOfInputRequests = 1;
                    else
                        TotalNumberOfInputRequests = (int)Math.Ceiling((double)totalNumberOfBytes / (double)transferSize);

                    if (totalNumberOfBytes % transferSize == 0)
                        TotalNumberOfInputRequests++;
                }
            }
            else
            {
                TotalNumberOfInputRequests = 1;
            }

            m_stopInputTransfers = false;
            m_dataOverrunOccurred = false;
            InputTransfersComplete = false;

            CreateBulkInRequestObjects();

            NumberOfInputRequestsSubmitted = 0;
            NumberOfInputRequestsCompleted = 0;

            m_inputXferTimeOut = 4 * (uint)(1000.0 * ((1.0 / (double)scanRate) * (double)transferSize));

            PrepareBulkInQueues(transferSize);

            m_processBulkInRequests = new Thread(new ThreadStart(ProcessBulkInRequests));
            m_processBulkInRequests.Priority = ThreadPriority.Normal;
            m_processBulkInRequests.Name = "ProcessBulkInRequests";
            m_processBulkInRequests.Start();

            while (!BulkInRequestsStarted)
			{
                Thread.Sleep(0);
			}
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method that allows a derived platform interop object to
        /// set up parameters for data transfer
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //===========================================================================================
        internal override void PrepareOutputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            m_errorCode = ErrorCodes.NoErrors;

            m_totalBytesReceivedByDevice = 0;
            m_driverInterfaceOutputBufferIndex = 0;
            m_totalNumberOfOutputBytesRequested = totalNumberOfBytes;
			m_totalNumberOfOutputBytesTransferred = 0;
			m_totalBytesReceivedByDevice = 0;
			m_stopOutputTransfers = false;

            if (totalNumberOfBytes >= m_criticalParams.OutputFifoSize)
                m_numberOfWorkingOutputRequests = Math.Max(4, (int)Math.Ceiling((double)m_criticalParams.OutputFifoSize / (double)transferSize));
            else
                m_numberOfWorkingOutputRequests = (int)Math.Ceiling((double)totalNumberOfBytes / (double)transferSize);

            CreateBulkOutRequestObjects();

            m_processBulkOutRequests = new Thread(new ThreadStart(ProcessBulkOutRequests));
            m_processBulkOutRequests.Priority = ThreadPriority.Normal;
            m_processBulkOutRequests.Name = "ProcessBulkOutRequests";
            m_processBulkOutRequests.Start();
        }

        //===================================================================================================
        /// <summary>
        /// Virual method for processing Bulk In reads
        /// </summary>
        //===================================================================================================
        internal virtual void ProcessBulkInRequests()
        {
        }

          //===================================================================================================
        /// <summary>
        /// Processes Bulk Out requests
        /// </summary>
        //===================================================================================================
        internal virtual void ProcessBulkOutRequests()
        {
            Monitor.Enter(m_bulkOutRequestLock);

			m_readyToStartOutputScan = false;
			m_errorCode = ErrorCodes.NoErrors;

			OutputTransfersComplete = false;
			
            if (m_deviceInfo.EndPointIn == 0)
            {
                m_errorCode = ErrorCodes.BulkInputTransfersNotSupported;
                return;
            }

            int bytesTransferred = 0;
            int status;

			// submit data to fill the DAC FIFO
            foreach (UsbBulkOutRequest br in m_bulkOutRequests)
            {
				m_totalNumberOfOutputBytesTransferred += br.BytesRequested;

				status = UsbBulkOutRequest(br, ref bytesTransferred);
				
				if (status == 0)
				{
					m_totalBytesReceivedByDevice += bytesTransferred;
				}
				else
				{
					if (status == -9)
						System.Diagnostics.Debug.Assert(false, "Underrun error occurred before device was started");
					
					m_errorCode = TranslateErrorCode(status);

                    break;
				}
            }

			if (m_errorCode == ErrorCodes.NoErrors)
			{
			    // indicate that the device is ready for the "START" command
			    m_readyToStartOutputScan = true;
    			
			    // wait for the device start
			    while (!m_readyToSubmitRemainingOutputTransfers)
			    {
				    Thread.Sleep(1);
			    }
    			
			    // submit remaining output transfers
			    int bytesToTransfer;
			    int bytesToCopyOnFirstPass;
			    int bytesToCopyOnSecondPass;
			    int sourceBufferLength = m_driverInterfaceOutputBuffer.Length;
    			
			    while (m_errorCode == 0 && !m_stopOutputTransfers && !OutputTransfersComplete)
			    {
            	    foreach (UsbBulkOutRequest br in m_bulkOutRequests)
            	    {
					    bytesToTransfer = br.Buffer.Length;
    					
					    if (m_criticalParams.OutputSampleMode == SampleMode.Finite)
					    {
						    if (m_totalNumberOfOutputBytesRequested - m_totalNumberOfOutputBytesTransferred < bytesToTransfer)
							    bytesToTransfer = m_totalNumberOfOutputBytesRequested - m_totalNumberOfOutputBytesTransferred;
					    }
    						
					    br.BytesRequested = bytesToTransfer;
    						
					    if ((m_criticalParams.OutputSampleMode == SampleMode.Continuous && !m_stopOutputTransfers) ||
						        (m_criticalParams.OutputSampleMode == SampleMode.Finite && (m_totalNumberOfOutputBytesTransferred < m_totalNumberOfOutputBytesRequested)))
					    {
						    if ((m_driverInterfaceOutputBufferIndex + bytesToTransfer) >= sourceBufferLength)
						    {
                                // two passes are required since the current input scan write index
                                // wrapped around to the beginning of the internal read buffer
                                bytesToCopyOnFirstPass = sourceBufferLength - m_driverInterfaceOutputBufferIndex;
                                bytesToCopyOnSecondPass = (int)bytesToTransfer - bytesToCopyOnFirstPass;
						    }
						    else
						    {
                                // only one pass is required since the current input scan write index
                                // did not wrap around
                                bytesToCopyOnFirstPass = (int)bytesToTransfer;
                                bytesToCopyOnSecondPass = 0;
						    }
    							
						    // copy data from driver interface's output buffer and transfer to the device
						    if (bytesToCopyOnFirstPass > 0)
                                Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, br.Buffer, 0, bytesToCopyOnFirstPass);

                            m_driverInterfaceOutputBufferIndex += bytesToCopyOnFirstPass;

                            if (m_driverInterfaceOutputBufferIndex >= m_driverInterfaceOutputBuffer.Length)
                                m_driverInterfaceOutputBufferIndex = 0;

                            if (bytesToCopyOnSecondPass > 0)
                                Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, br.Buffer, bytesToCopyOnFirstPass, bytesToCopyOnSecondPass);

                            m_driverInterfaceOutputBufferIndex += bytesToCopyOnSecondPass;

						    m_totalNumberOfOutputBytesTransferred += br.BytesRequested;
    				
						    status = UsbBulkOutRequest(br, ref bytesTransferred);
    				
						    if (status == 0)
						    {
							    m_totalBytesReceivedByDevice += bytesTransferred;
						    }
						    else
						    {
                                m_errorCode = ErrorCodes.BulkOutTransferError;

                                break;
						    }
					    }
					    else if (m_criticalParams.OutputSampleMode == SampleMode.Finite)
					    {
						    if (m_totalNumberOfOutputBytesTransferred == m_totalNumberOfOutputBytesRequested)
						    {
							    OutputTransfersComplete = true;
							    break;
						    }
					    }
				    }
    				
				    Thread.Sleep(1);
			    }
			}
			// exiting
			
			m_readyToStartOutputScan = false;

            if (m_errorCode != ErrorCodes.NoErrors)
                OnOutputErrorCleanup();
			
            Monitor.Exit(m_bulkOutRequestLock);
        }

		//=================================================================================
        /// <summary>
        /// Translates the error code to a MBD error code
        /// </summary>
        /// <param name="libusbErrorCode">The libusb error code</param>
        /// <returns>The MBD error code</returns>
        //=================================================================================
        protected virtual ErrorCodes TranslateErrorCode(int errorCode)
        {
			return ErrorCodes.NoErrors;
		}
		
        protected object queueBufferLock = new object();

        //================================================================================================
        /// <summary>
        /// Enqueues and dequeues bulk in buffers. Separate threads enqueue and dequeue bulk in buffers
        /// so they are synchronized here
        /// </summary>
        /// <param name="bulkInBuffer">The bulk in buffer to enqueue</param>
        /// <param name="queueAction">The queue action - Enqueue or Dequeue</param>
        /// <returns>The bulk in buffer that was dequeued</returns>
        //================================================================================================
        protected byte[] QueueBuffer(byte[] bulkInBuffer, QueueAction queueAction)
        {
            lock (queueBufferLock)
            {
                if (queueAction == QueueAction.Enqueue)
                {
                    m_completedBulkInRequestBuffers.Enqueue(bulkInBuffer);
                    return null;
                }
                else
                {
                    return m_completedBulkInRequestBuffers.Dequeue();
                }
            }
        }

        //==================================================================
        /// <summary>
        /// Virtual method for a USB control IN request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbControlInRequest(UsbSetupPacket packet)
        {
            System.Diagnostics.Debug.Assert(false, "UsbControlInRequest not implemented in SynchronousUsbInterop");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //==================================================================
        /// <summary>
        /// Virtual method for a USB control OUT request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbControlOutRequest(UsbSetupPacket packet)
        {
            System.Diagnostics.Debug.Assert(false, "UsbControlOutRequest not implemented in SynchronousUsbInterop");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //==============================================================================================
        /// <summary>
        /// Virtual method for a USB Bulk IN request
        /// </summary>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <param name="bytesRequested">The number of bytes to requested</param>
        /// <param name="bytesReceived">The number of actual bytes received</param>
        /// <returns>The result</returns>
        //==============================================================================================
        internal override ErrorCodes UsbBulkInRequest(ref BulkInBuffer buffer, ref uint bytesReceived)
        {
			ErrorCodes result;
            BulkInBuffer bulkInBuffer = null;

            do
            {
                result = m_errorCode;

                if (result != ErrorCodes.NoErrors)
                    break;

                if (this.m_bulkInCompletedBuffers.Count > 0)
                    bulkInBuffer = QueueBulkInCompletedBuffers(null, QueueAction.Dequeue);
                else
                    Thread.Sleep(1);

            } while (bulkInBuffer == null && !m_stopInputTransfers);

            if (bulkInBuffer != null)
            {
				buffer = bulkInBuffer;
                bytesReceived = (uint)bulkInBuffer.Length;
            }
            else
            {
                buffer = null;
                bytesReceived = 0;
            }

            return result;
        }

        //==================================================================
        /// <summary>
        /// Virtual method for a USB Bulk OUT request
        /// </summary>
        /// <param name="buffer">The buffer containing the data to send</param>
        /// <param name="count">The number of samples to send</param>
        /// <returns>The result</returns>
        //==================================================================
        internal override int UsbBulkOutRequest(UsbBulkOutRequest br, ref int bytesTransferred)
        {
            System.Diagnostics.Debug.Assert(false, "UsbBulkOutRequest not implemented in SynchronousUsbInterop");
            return 0;
        }
    }
}
