using UnityEngine;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using TMPro;
using UnityEngine.UI;

public class Page3_DB : MonoBehaviour
{
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    [Header("Input Object")]
    public TMP_InputField title;
    public TMP_InputField author;
    public TMP_InputField publisher;
    public TMP_InputField price;
    public TMP_InputField url;
    public TMP_InputField isbn;
    public TMP_InputField description;

    public void DataBaseConnection()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("INSERT_BOOK", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("B_TITLE", OracleDbType.NVarchar2).Value = title.text;
            command.Parameters.Add("B_AUTHOR", OracleDbType.NVarchar2).Value = author.text;
            command.Parameters.Add("B_PUBLISHER", OracleDbType.NVarchar2).Value = publisher.text;
            command.Parameters.Add("B_PRICE", OracleDbType.Decimal).Value = price.text;
            command.Parameters.Add("B_URL", OracleDbType.Varchar2).Value = url.text;
            command.Parameters.Add("B_BOOKPHOTO", OracleDbType.Blob).Value = DBNull.Value;
            command.Parameters.Add("B_ISBN", OracleDbType.Decimal).Value = isbn.text;
            command.Parameters.Add("B_ISBNPHOTO", OracleDbType.Blob).Value = DBNull.Value;
            command.Parameters.Add("B_DESCRIPTION", OracleDbType.NVarchar2).Value = description.text;

            command.ExecuteNonQuery();

            Debug.Log("Data inserted successfully.");
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }

    /*
    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }
    */
}
